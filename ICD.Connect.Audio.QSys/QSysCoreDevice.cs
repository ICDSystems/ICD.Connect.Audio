using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Controls;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys
{
	public sealed class QSysCoreDevice : AbstractDevice<QSysCoreDeviceSettings>
	{
		private const char DELIMITER = '\x00';

		/// <summary>
		/// KeepAlive Interval is how often the NoOp RPC is sent
		/// NoOp is sent to perform no operation, but to just keep the socket alive
		/// 29 Seconds makes sure at least 2 are sent in the 60 second window
		/// </summary>
		private const long KEEPALIVE_INTERVAL = 29 * 1000;

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private bool m_Initialized;
		private readonly SafeTimer m_OnlineNoOpTimer;

		/// <summary>
		/// Change Groups
		/// </summary>
		private Dictionary<string, IChangeGroup> m_ChangeGroups;
		private Dictionary<int, IChangeGroup> m_ChangeGroupsById;
		private readonly SafeCriticalSection m_ChangeGroupsCriticalSection;

		/// <summary>
		/// Named Controls
		/// </summary>
		private Dictionary<string, INamedControl> m_NamedControls;

		private Dictionary<int, INamedControl> m_NamedControlsById;
		private readonly SafeCriticalSection m_NamedControlsCriticalSection;

		/// <summary>
		/// Named Components
		/// </summary>
		private Dictionary<string, INamedComponent> m_NamedComponents;
		private Dictionary<int, INamedComponent> m_NamedComponentsById;
		private readonly SafeCriticalSection m_NamedComponentsCriticalSection;

		private readonly ISerialBuffer m_SerialBuffer;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		/// <summary>
		/// Configuration Path for reload
		/// </summary>
		private string m_ConfigPath;

		private readonly IcdHashSet<IDeviceControl> m_LoadedControls;
		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// Username for logging in to the device.
		/// </summary>
		[PublicAPI]
		public string Username { get; set; }

		/// <summary>
		/// Password for logging in to the device.
		/// </summary>
		[PublicAPI]
		public string Password { get; set; }

		/// <summary>
		/// Device Initialized Status.
		/// </summary>
		public bool Initialized
		{
			get { return m_Initialized; }
			private set
			{
				if (value == m_Initialized)
					return;

				m_Initialized = value;

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_LoadedControls = new IcdHashSet<IDeviceControl>();

			Controls.Add(new QSysCoreRoutingControl(this, 0));

			m_OnlineNoOpTimer = SafeTimer.Stopped(SendNoOpKeepalive);

			m_SerialBuffer = new JsonSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ChangeGroupsCriticalSection = new SafeCriticalSection();
			m_ChangeGroups = new Dictionary<string, IChangeGroup>();
			m_ChangeGroupsById = new Dictionary<int, IChangeGroup>();

			m_NamedControlsCriticalSection = new SafeCriticalSection();
			m_NamedControls = new Dictionary<string, INamedControl>();
			m_NamedControlsById = new Dictionary<int, INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<string, INamedComponent>();
			m_NamedComponentsById = new Dictionary<int, INamedComponent>();

			m_ConnectionStateManager = new ConnectionStateManager(this){ConfigurePort = ConfigurePort};
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;

			Unsubscribe(m_SerialBuffer);

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived -= PortOnSerialDataReceived;
			m_ConnectionStateManager.Dispose();
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(ISerialPort port)
		{
			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
		}

		protected override void UpdateCachedOnlineStatus()
		{
			base.UpdateCachedOnlineStatus();


			if (m_OnlineNoOpTimer != null)
			{
				if (IsOnline)
					m_OnlineNoOpTimer.Reset(KEEPALIVE_INTERVAL, KEEPALIVE_INTERVAL);
				else
					m_OnlineNoOpTimer.Stop();
			}
		}

		public void AddChangeGroup(IEnumerable<IChangeGroup> changeGroups)
		{
			changeGroups.ForEach(AddChangeGroup);
		}

		public void AddChangeGroup(IChangeGroup changeGroup)
		{
			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				m_ChangeGroups.Add(changeGroup.ChangeGroupId, changeGroup);
				m_ChangeGroupsById.Add(changeGroup.Id, changeGroup);
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}
		}

		public IChangeGroup GetChangeGroupById(int id)
		{
			IChangeGroup changeGroup;

			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				if (!m_ChangeGroupsById.TryGetValue(id, out changeGroup))
					return null;
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}

			return changeGroup;
		}

		public IChangeGroup GetChangeGroup(string changeGroupId)
		{
			IChangeGroup changeGroup;

			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				if (!m_ChangeGroups.TryGetValue(changeGroupId, out changeGroup))
					return null;
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}

			return changeGroup;
		}

		public void AddNamedControl(IEnumerable<INamedControl> namedControls)
		{
			namedControls.ForEach(AddNamedControl);
		}

		public void AddNamedControl(INamedControl namedControl)
	    {
	        m_NamedControlsCriticalSection.Enter();

	        try
	        {
	            m_NamedControls.Add(namedControl.ControlName, namedControl);
		        m_NamedControlsById.Add(namedControl.Id, namedControl);
	        }
	        finally
	        {
	            m_NamedControlsCriticalSection.Leave();
	        }
	    }

		public void AddNamedComponent(IEnumerable<INamedComponent> namedComponents)
		{
			namedComponents.ForEach(AddNamedComponent);
		}

		public void AddNamedComponent(INamedComponent namedComponent)
		{
			m_NamedComponentsCriticalSection.Enter();
			try
			{
				m_NamedComponents.Add(namedComponent.ComponentName, namedComponent);
				m_NamedComponentsById.Add(namedComponent.Id, namedComponent);
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}
		}

		public void AddKrangControl(IEnumerable<IQSysKrangControl> krangControls)
		{
			krangControls.ForEach(AddKrangControl);
		}

		public void AddKrangControl(IQSysKrangControl krangControl)
		{
			Controls.Add(krangControl);
		}

		public void LoadControls(string path)
		{
			m_ConfigPath = path;

			string fullPath = PathUtils.GetDefaultConfigPath("QSys", path);

			try
			{
				string xml = IcdFile.ReadToEnd(fullPath, new UTF8Encoding(false));
				xml = EncodingUtils.StripUtf8Bom(xml);

				ParseXml(xml);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, e, "{0} - Failed to load integration config {1} - {2}", this, fullPath, e.Message);
			}
		}

		public void ReloadControls()
		{
			LoadControls(m_ConfigPath);
		}

		public IEnumerable<IChangeGroup> GetChangeGroups()
		{
			List<IChangeGroup> changeGroups = null;
			m_ChangeGroupsCriticalSection.Execute(() => changeGroups = m_ChangeGroups.Values.ToList(m_ChangeGroups.Count));
			return changeGroups;
		}

		public IEnumerable<INamedControl> GetNamedControls()
		{
			List<INamedControl> namedControls = null;
			m_NamedControlsCriticalSection.Execute(() => namedControls = m_NamedControls.Values.ToList(m_NamedControls.Count));
			return namedControls;
		}

		public IEnumerable<INamedComponent> GetNamedComponents()
		{
			List<INamedComponent> namedComponents = null;
			m_NamedComponentsCriticalSection.Execute(() => namedComponents = m_NamedComponents.Values.ToList(m_NamedComponents.Count));
			return namedComponents;
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Sends the data to the device and calls the callback asynchronously with the response.
		/// </summary>
		/// <param name="json"></param>
		internal void SendData(string json)
		{
			// Pad with the delimiter
			if (!json.EndsWith(DELIMITER))
				json = json + DELIMITER;

			m_ConnectionStateManager.Send(json);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		/// <summary>
		/// Initialize the CODEC.
		/// </summary>
		private void Initialize()
		{
			m_ChangeGroupsCriticalSection.Execute(() => m_ChangeGroups.ForEach(k => k.Value.Initialize()));

			Initialized = true;
		}

		/// <summary>
		/// Sends a no-op RPC command to keep the connection alive
		/// </summary>
		private void SendNoOpKeepalive()
		{
            if (!m_ConnectionStateManager.IsConnected)
                return;

			SendData(new NoOpRpc().Serialize());
		}

		private void ParseXml(string xml)
		{
			DisposeLoadedControls();

			CoreElementsLoadContext loadContext = CoreElementsXmlUtils.GetControlsFromXml(xml, this);

			// Add to correct collections
			AddChangeGroup(loadContext.GetChangeGroups());
			AddNamedControl(loadContext.GetNamedControls());
			AddNamedComponent(loadContext.GetNamedComponents());
			AddKrangControl(loadContext.GetKrangControls());
		}

		private void DisposeLoadedControls()
		{
			// Clear Change Groups
			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, IChangeGroup> kvp in m_ChangeGroups)
				{
					kvp.Value.DestroyChangeGroup();
					kvp.Value.Dispose();
				}
				m_ChangeGroups = new Dictionary<string, IChangeGroup>();
				m_ChangeGroupsById = new Dictionary<int, IChangeGroup>();
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}

			// Clear Named Controls
			m_NamedControlsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedControl> kvp in m_NamedControls)
					kvp.Value.Dispose();
				m_NamedControls = new Dictionary<string, INamedControl>();
				m_NamedControlsById = new Dictionary<int, INamedControl>();
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}

			// Clear Named Components
			m_NamedComponentsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedComponent> kvp in m_NamedComponents)
					kvp.Value.Dispose();
				m_NamedComponents = new Dictionary<string, INamedComponent>();
				m_NamedComponentsById = new Dictionary<int, INamedComponent>();
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			// Clear Controls Collection
			foreach (IDeviceControl control in m_LoadedControls)
			{
				control.Dispose();
				Controls.Remove(control.Id);
			}

			m_LoadedControls.Clear();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Called when we receive data from the port.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void PortOnSerialDataReceived(object sender, StringEventArgs stringEventArgs)
		{
			string data = stringEventArgs.Data;

			// Ignore empty change groups
			if (data ==
			    @"{""jsonrpc"":""2.0"",""method"":""ChangeGroup.Poll"",""params"":{""Id"":""AutoChangeGroup"",""Changes"":[]}}")
				return;

			m_SerialBuffer.Enqueue(data);
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			m_SerialBuffer.Clear();

			if (args.Data)
				Initialize();
			else
			{
				Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}
		}

		/// <summary>
		/// Called when the port online status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Serial Buffer Callbacks

		/// <summary>
		/// Subscribes to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribes from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferOnCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a full message from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void BufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			JObject json;

			try
			{
				json = JObject.Parse(stringEventArgs.Data);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to parse data - {0}{1}{2}", e.GetType().Name,
				    IcdEnvironment.NewLine, JsonUtils.Format(stringEventArgs.Data));
				return;
			}

			string responseMethod = (string)json.SelectToken("method");
			if (string.Equals(responseMethod, "ChangeGroup.Poll", StringComparison.OrdinalIgnoreCase))
			{
				ParseChangeGroupResponse(json);
				return;
			}

			string responseId = (string)json.SelectToken("id");

			switch (responseId)
			{
				case (RpcUtils.RPCID_NO_OP):
					return;
				case (RpcUtils.RPCID_NAMED_CONTROL_GET):
					ParseNamedControlGetResponse(json);
					return;
				case (RpcUtils.RPCID_NAMED_CONTROL_SET):
					ParseNamedControlSetResponse(json);
					break;
				case (RpcUtils.RPCID_NAMED_COMPONENT_GET):
					ParseNamedComponentGetResponse(json);
					break;

			}
		}

		private void ParseNamedControlSetResponse(JToken json)
		{
			JToken result = json.SelectToken("result");
			if (result != null && result.HasValues)
				ParseNamedControlResponse(result);
		}

		private void ParseChangeGroupResponse(JToken json)
		{
			JToken responseParams = json.SelectToken("params");
			if (!responseParams.HasValues)
				return;

			JToken changes = responseParams.SelectToken("Changes");
			if (!changes.HasValues)
				return;

			foreach (JToken change in changes)
			{
				JToken component = change.SelectToken("Component");
				if (component != null)
					ParseNamedComponentChangeGroupResponse(change);
				else
				{
					ParseNamedControlResponse(change);
				}
			}
		}

		private void ParseNamedComponentChangeGroupResponse(JToken result)
		{
			string nameToken = (string)result.SelectToken("Component");

			if (string.IsNullOrEmpty(nameToken))
				return;

			INamedComponent component;

			m_NamedComponentsCriticalSection.Enter();
			try
			{
				if (!m_NamedComponents.TryGetValue(nameToken, out component))
					return;
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			component.ParseFeedback(result);
		}

		private void ParseNamedComponentGetResponse(JToken response)
		{
			JToken result = response.SelectToken("result");
			if (result == null || !result.HasValues)
				return;

			string nameToken = (string)result.SelectToken("Name");

			if (string.IsNullOrEmpty(nameToken))
				return;

			INamedComponent component;

			m_NamedComponentsCriticalSection.Enter();
			try
			{
				if (!m_NamedComponents.TryGetValue(nameToken, out component))
					return;
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			JToken controls = result.SelectToken("Controls");
			if (!controls.HasValues)
				return;

			foreach (JToken control in controls)
				component.ParseFeedback(control);
		}

		/// <summary>
        /// Parses one or more Named Controls, and sets the values on the controls
        /// </summary>
        /// <param name="json"></param>
	    private void ParseNamedControlGetResponse(JObject json)
	    {
	        JToken results = json.SelectToken("result");
	        if (results == null || !results.HasValues)
	            return;

			foreach (JToken result in results)
				ParseNamedControlResponse(result);
	    }

        /// <summary>
        /// Parses a single named control, and sets the values on the control
        /// </summary>
        /// <param name="result"></param>
	    private void ParseNamedControlResponse(JToken result)
	    {
	        string nameToken = (string)result.SelectToken("Name");

		    if (string.IsNullOrEmpty(nameToken))
			    return;

	        INamedControl control;

            m_NamedControlsCriticalSection.Enter();

	        try
	        {
	            if (!m_NamedControls.TryGetValue(nameToken, out control))
	                return;
	        }
	        finally
	        {
	            m_NamedControlsCriticalSection.Leave();
	        }

			control.ParseFeedback(result);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Username = null;
			Password = null;
			m_ConfigPath = null;

			DisposeLoadedControls();

			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(QSysCoreDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Username = Username;
			settings.Password = Password;
			settings.Config = m_ConfigPath;
			settings.Port = m_ConnectionStateManager.PortNumber;

			settings.Copy(m_NetworkProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(QSysCoreDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Username = settings.Username;
			Password = settings.Password;
			m_ConfigPath = settings.Config;

			m_NetworkProperties.Copy(settings);

			// Load the config
			if (!string.IsNullOrEmpty(settings.Config))
				LoadControls(settings.Config);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
				}
			}

			m_ConnectionStateManager.SetPort(port);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Loaded Config", m_ConfigPath);
			addRow("Connected", m_ConnectionStateManager.IsConnected);
			addRow("Initialized", Initialized);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("GetStatus", "Gets Core Status", () => SendData(new StatusGetRpc().Serialize()));
			yield return
				new ConsoleCommand("GetComponents", "Gets Components in Design",
				                   () => SendData(new ComponentGetComponentsRpc().Serialize()));
			yield return new ConsoleCommand("ReloadControls", "Reload controls from previous file", () => ReloadControls());
			yield return
				new GenericConsoleCommand<string>("LoadControls", "Load Controls from Specified File", p => LoadControls(p));

		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;
			yield return ConsoleNodeGroup.KeyNodeMap("ChangeGroups", GetChangeGroups(), c => (uint)c.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("NamedControls", GetNamedControls(), c => (uint)c.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("NamedComponents", GetNamedComponents(), c => (uint)c.Id);

		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
