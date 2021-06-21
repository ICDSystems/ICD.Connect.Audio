using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;
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

namespace ICD.Connect.Audio.QSys.Devices.QSysCore
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

		private readonly QSysCoreComponentsCollection m_Components;

		private bool m_Initialized;
		private readonly SafeTimer m_OnlineNoOpTimer;

		private readonly ISerialBuffer m_SerialBuffer;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		/// <summary>
		/// Configuration Path for reload
		/// </summary>
		private string m_ConfigPath;

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

		/// <summary>
		/// Gets the components collection.
		/// </summary>
		public QSysCoreComponentsCollection Components { get { return m_Components; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_Components = new QSysCoreComponentsCollection(this);

			m_OnlineNoOpTimer = SafeTimer.Stopped(SendNoOpKeepalive);

			m_SerialBuffer = new JsonSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this) {ConfigurePort = ConfigurePort};
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
			m_ConnectionStateManager.SetPort(port, false);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IPort port)
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

			if (m_OnlineNoOpTimer == null)
				return;

			if (IsOnline)
				m_OnlineNoOpTimer.Reset(KEEPALIVE_INTERVAL, KEEPALIVE_INTERVAL);
			else
				m_OnlineNoOpTimer.Stop();
		}

		public void LoadControls(string path)
		{
			Components.ClearLoadedControls();

			m_ConfigPath = path;

			string fullPath = PathUtils.GetDefaultConfigPath("QSys", path);

			if (!IcdFile.Exists(fullPath))
			{
				Logger.Log(eSeverity.Error, "Failed to load integration config {0} - Path does not exist", fullPath);
				return;
			}

			try
			{
				string xml = IcdFile.ReadToEnd(fullPath, new UTF8Encoding(false));
				xml = EncodingUtils.StripUtf8Bom(xml);

				Components.ParseXml(xml);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Failed to load integration config {0} - {1}", fullPath, e.Message);
			}
		}

		public void ReloadControls()
		{
			LoadControls(m_ConfigPath);
		}

		public void GetNamedComponents()
		{
			SendData(new ComponentGetComponentsRpc());
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Sends the data to the device and calls the callback asynchronously with the response.
		/// </summary>
		/// <param name="data"></param>
		internal void SendData(IRpc data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			string json = data.Serialize();
			SendData(json);
		}

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
		/// Initialize the DSP.
		/// </summary>
		private void Initialize()
		{
			Components.Initialize();

			GetNamedComponents();

			Initialized = true;
		}

		/// <summary>
		/// Sends a no-op RPC command to keep the connection alive
		/// </summary>
		private void SendNoOpKeepalive()
		{
			if (!m_ConnectionStateManager.IsConnected)
				return;

			SendData(new NoOpRpc());
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
			if (data.Contains(@"{""jsonrpc"":""2.0"",""method"":""ChangeGroup.Poll"",""params"":{""Id"":""AutoChangeGroup"",""Changes"":[]}}"))
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
				Logger.Log(eSeverity.Critical, "Lost connection");
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
				Logger.Log(eSeverity.Error, "Failed to parse data - {0}{1}{2}", e.GetType().Name,
				           IcdEnvironment.NewLine, stringEventArgs.Data);
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
				case (RpcUtils.RPCID_NAMED_COMPONENTS_GET_ALL):
					ParseNamedComponentsGetAllResponse(json);
					break;
			}
		}

		private void ParseNamedControlSetResponse([NotNull] JToken json)
		{
			if (json == null)
				throw new ArgumentNullException("json");

			JToken result = json.SelectToken("result");
			if (result != null && result.HasValues)
				ParseNamedControlResponse(result);
		}

		private void ParseChangeGroupResponse([NotNull] JToken json)
		{
			if (json == null)
				throw new ArgumentNullException("json");

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
					ParseNamedControlResponse(change);
			}
		}

		private void ParseNamedComponentChangeGroupResponse([NotNull] JToken result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			string nameToken = (string)result.SelectToken("Component");

			if (string.IsNullOrEmpty(nameToken))
				return;

			INamedComponent component;
			if (Components.TryGetNamedComponent(nameToken, out component))
				component.ParseFeedback(result);
		}

		private void ParseNamedComponentGetResponse([NotNull] JToken response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			JToken result = response.SelectToken("result");
			if (result == null || !result.HasValues)
				return;

			string nameToken = (string)result.SelectToken("Name");

			if (string.IsNullOrEmpty(nameToken))
				return;

			INamedComponent component;
			if (!Components.TryGetNamedComponent(nameToken, out component))
				return;

			JToken controls = result.SelectToken("Controls");
			if (controls == null || !controls.HasValues)
				return;

			foreach (JToken control in controls)
				component.ParseFeedback(control);
		}

		private void ParseNamedComponentsGetAllResponse([NotNull] JToken response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			JToken result = response.SelectToken("result");

			foreach(JToken control in result)
			{
				JToken properties = control.SelectToken("Properties");
				if (properties == null || !properties.HasValues)
					continue;

				string nameToken = (string)control.SelectToken("Name");
				INamedComponent component;
				if (!Components.TryGetNamedComponent(nameToken, out component))
					continue;

				foreach(JToken property in properties)
					component.ParsePropertyFeedback(property);
			}
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
			if (Components.TryGetNamedControl(nameToken, out control))
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

			Components.ClearLoadedControls();

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
					Logger.Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
				}
			}

			SetPort(port);
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_ConnectionStateManager.Start();
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(QSysCoreDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new QSysCoreRoutingControl(this, 0));
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

			yield return new ConsoleCommand("GetStatus", "Gets Core Status", () => SendData(new StatusGetRpc()));
			yield return
				new ConsoleCommand("GetComponents", "Gets Components in Design", () => GetNamedComponents());
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

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (m_ConnectionStateManager != null)
				yield return m_ConnectionStateManager.Port;

			yield return ConsoleNodeGroup.KeyNodeMap("ChangeGroups", Components.GetChangeGroups(), c => (uint)c.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("NamedControls", Components.GetNamedControls(), c => (uint)c.Id);
			yield return ConsoleNodeGroup.KeyNodeMap("NamedComponents", Components.GetNamedComponents(), c => (uint)c.Id);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
