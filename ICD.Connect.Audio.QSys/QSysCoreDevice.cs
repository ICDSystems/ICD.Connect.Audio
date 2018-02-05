using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControl;
using ICD.Connect.Audio.QSys.CoreControl.ChangeGroup;
using ICD.Connect.Audio.QSys.CoreControl.NamedComponent;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using ICD.Connect.Audio.QSys.Rpc;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Heartbeat;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys
{
	public sealed class QSysCoreDevice : AbstractDevice<QSysCoreDeviceSettings>, IConnectable
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

		/// <summary>
		/// Raised when the codec becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private bool m_Initialized;
		private bool m_IsConnected;
		private ISerialPort m_Port;
	    private readonly SafeTimer m_OnlineNoOpTimer;

		/// <summary>
		/// Change Groups
		/// </summary>
		private Dictionary<string, ChangeGroup> m_ChangeGroups;
		private Dictionary<int, ChangeGroup> m_ChangeGroupsById;
		private readonly SafeCriticalSection m_ChangeGroupsCriticalSection;

		/// <summary>
		/// Named Controls
		/// </summary>
	    private Dictionary<string, INamedControl> m_NamedControls;
		private Dictionary<int, INamedControl> m_NamedControlsById;
	    private readonly SafeCriticalSection m_NamedControlsCriticalSection;

		private Dictionary<string, INamedComponent> m_NamedComponents;
		private readonly SafeCriticalSection m_NamedComponentsCriticalSection;

        private readonly ISerialBuffer m_SerialBuffer;

        /// <summary>
        /// Configuration Path for reload
        /// </summary>
		private string m_ConfigPath;

	    #region Properties

		public Heartbeat Heartbeat { get; private set; }

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
		/// Returns true when the core is connected.
		/// </summary>
		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				//todo: clean this up

				if (m_IsConnected)
				{
					m_ChangeGroupsCriticalSection.Execute(() => m_ChangeGroups.ForEach(k => k.Value.Initialize()));
				}

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

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
			Heartbeat = new Heartbeat(this);
		    m_OnlineNoOpTimer = SafeTimer.Stopped(SendNoOpKeepalive);

            m_SerialBuffer = new DelimiterSerialBuffer(DELIMITER);
			Subscribe(m_SerialBuffer);

			m_ChangeGroupsCriticalSection = new SafeCriticalSection();
			m_ChangeGroups = new Dictionary<string, ChangeGroup>();
			m_ChangeGroupsById = new Dictionary<int, ChangeGroup>();

            m_NamedControlsCriticalSection = new SafeCriticalSection();
            m_NamedControls = new Dictionary<string, INamedControl>();
			m_NamedControlsById = new Dictionary<int, INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<string, INamedComponent>();
		}

	    #region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			Heartbeat.StopMonitoring();
			Heartbeat.Dispose();

			Unsubscribe(m_SerialBuffer);
			Unsubscribe(m_Port);

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Connect to the device.
		/// </summary>
		[PublicAPI]
		public void Connect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect, port is null");
				return;
			}

			m_Port.Connect();
			IsConnected = m_Port.IsConnected;
		}

		/// <summary>
		/// Disconnect from the device.
		/// </summary>
		[PublicAPI]
		public void Disconnect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to disconnect, port is null");
				return;
			}

			m_Port.Disconnect();
			IsConnected = m_Port.IsConnected;
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			if (port == m_Port)
				return;

			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			if (m_Port != null)
				Disconnect();

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			if (m_Port != null)
				Connect();

			Heartbeat.StartMonitoring();

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Configures a com port for communication with the hardware.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate115200,
								eComDataBits.ComspecDataBits8,
								eComParityType.ComspecParityNone,
								eComStopBits.ComspecStopBits1,
								eComProtocolType.ComspecProtocolRS232,
								eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
								eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
								false);
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

		public void AddNamedControlToChangeGroupById(int changeGroupId, AbstractNamedControl control)
		{
			ChangeGroup changeGroup = GetChangeGroupById(changeGroupId);

			// todo: log or exception here
			if (changeGroup == null)
				return;

			changeGroup.AddNamedControl(control);
		}

		public void AddChangeGroup(ChangeGroup changeGroup)
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

		public ChangeGroup GetChangeGroupById(int id)
		{
			ChangeGroup changeGroup;

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

		public ChangeGroup GetChangeGroup(string changeGroupId)
		{
			ChangeGroup changeGroup;

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

		public void AddNamedControl(AbstractNamedControl namedControl)
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

		public void AddNamedComponent(INamedComponent namedComponent)
		{
			m_NamedComponentsCriticalSection.Enter();
			try
			{
				m_NamedComponents.Add(namedComponent.ComponentName, namedComponent);
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}
		}

		public void LoadControls(string path)
		{
			m_ConfigPath = path;

			string fullPath = PathUtils.GetDefaultConfigPath("QSys", path);

			try
			{
				string xml = IcdFile.ReadToEnd(fullPath, Encoding.UTF8);
				ParseXml(xml);
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "Failed to load integration config {0} - {1}", fullPath, e.Message);
			}
		}

		public void ReloadControls()
		{
			LoadControls(m_ConfigPath);
		}

		public IEnumerable<INamedControl> GetNamedControls()
		{
			List<INamedControl> controls;

			m_NamedControlsCriticalSection.Enter();
			try
			{
				controls = m_NamedControls.Values.ToList();
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}

			return controls;
		}

		public INamedControl GetNamedControlById(int id)
		{
			INamedControl control = null;
			m_NamedControlsCriticalSection.Execute(() => m_NamedControlsById.TryGetValue(id, out control));
			return control;

		}

		#endregion

        #region Internal Methods

        /// <summary>
        /// Sends the data to the device and calls the callback asynchronously with the response.
        /// </summary>
        /// <param name="json"></param>
        internal void SendData(string json)
		{
			//JsonUtils.Print(json);

			if (!IsConnected)
			{
				Log(eSeverity.Warning, "Device disconnected, attempting reconnect");
				Connect();
			}

			if (!IsConnected)
			{
				Log(eSeverity.Critical, "Unable to communicate with device");
				return;
			}

			// Pad with the delimiter
			if (!json.EndsWith(DELIMITER))
				json = json + DELIMITER;

			m_Port.Send(json);
		}

		/// <summary>
		/// Logs the message.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);
			message = AddLogPrefix(message);

			Logger.AddEntry(severity, message);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsConnected;
		}

		/// <summary>
		/// Initialize the CODEC.
		/// </summary>
		private void Initialize()
		{
			Initialized = true;
		}

		/// <summary>
		/// Returns the log message with a LutronQuantumNwkDevice prefix.
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		private string AddLogPrefix(string log)
		{
			return string.Format("{0} - {1}", this, log);
		}

        /// <summary>
        /// Sends a no-op RPC command to keep the connection alive
        /// </summary>
	    private void SendNoOpKeepalive()
        {
            if (!IsConnected)
                return;

            SendData(new NoOpRpc().Serialize());
        }

		private void ParseXml(string xml)
		{
			DisposeControls();

			//Parse Change Groups
			string changeGroupXml;
			if (XmlUtils.TryGetChildElementAsString(xml, "ChangeGroups", out changeGroupXml))
			{
				foreach (ChangeGroup changeGroup in CoreControlsXmlUtils.GetChangeGroupsFromXml(changeGroupXml, this))
					AddChangeGroup(changeGroup);
			}

			//Parse Named Controls
			string namedControlsXml;
			if (XmlUtils.TryGetChildElementAsString(xml, "NamedControls", out namedControlsXml))
				foreach (AbstractNamedControl control in CoreControlsXmlUtils.GetNamedControlsFromXml(namedControlsXml, this))
					AddNamedControl(control);

			//Parse Named Components
			string namedComponentsXml;
			if (XmlUtils.TryGetChildElementAsString(xml, "NamedComponents", out namedComponentsXml))
				foreach (INamedComponent component in CoreControlsXmlUtils.GetNamedComponentsFromXml(namedComponentsXml, this))
					AddNamedComponent(component);

			string krangControlsXml;
			if (XmlUtils.TryGetChildElementAsString(xml, "KrangControls", out krangControlsXml))
				foreach (IDeviceControl control in CoreControlsXmlUtils.GetKrangControlsFromXml(krangControlsXml, this))
					AddKrangControl(control);

		}

		private void AddKrangControl(IDeviceControl control)
		{
			Controls.Add(control);
		}

		private void DisposeControls()
		{
			// Clear Change Groups
			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, ChangeGroup> kvp in m_ChangeGroups)
				{
					kvp.Value.DestroyChangeGroup();
					kvp.Value.Dispose();
				}
				m_ChangeGroups = new Dictionary<string, ChangeGroup>();
				m_ChangeGroupsById = new Dictionary<int, ChangeGroup>();
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
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}

			// Clear Controls Collection
			Controls.Clear();
		}

		#endregion

        #region Port Callbacks

        /// <summary>
        /// Subscribes to the port events.
        /// </summary>
        /// <param name="port"></param>
        private void Subscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			port.OnSerialDataReceived += PortOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			port.OnSerialDataReceived -= PortOnSerialDataReceived;
		}

		/// <summary>
		/// Called when we receive data from the port.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void PortOnSerialDataReceived(object sender, StringEventArgs stringEventArgs)
		{
			m_SerialBuffer.Enqueue(stringEventArgs.Data);
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			m_SerialBuffer.Clear();

			IsConnected = args.Data;

			if (IsConnected)
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
		/// <param name="boolEventArgs"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs boolEventArgs)
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
			//JsonUtils.Print(stringEventArgs.Data);

		    JObject json = JObject.Parse(stringEventArgs.Data);

			string responseMethod = (string)json.SelectToken("method");

			if (!string.IsNullOrEmpty(responseMethod) &&
			    string.Equals(responseMethod.ToLower(), "ChangeGroup.Poll", StringComparison.OrdinalIgnoreCase))
			{
				ParseChangeGroupResponse(json);
				return;
			}

            string responseId = (string)json.SelectToken("id");

			if (!String.IsNullOrEmpty(responseId))
			{
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

				}
			}
		}

		private void ParseNamedControlSetResponse(JToken json)
		{
			JToken result = json.SelectToken("result");
			if (result != null && result.HasValues)
				ParseNamedControl(result);
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
				if (component != null && component.HasValues)
					ParseNamedComponent(change);
				else
				{
					ParseNamedControl(change);
				}
			}

		}

		private void ParseNamedComponent(JToken json)
		{
			throw new NotImplementedException();
		}

		/// <summary>
        /// Parses one or more Named Controls, and sets the values on the controls
        /// </summary>
        /// <param name="json"></param>
	    private void ParseNamedControlGetResponse(JObject json)
	    {
	        JToken results = json.SelectToken("result");
	        if (!results.HasValues)
	            return;
	        foreach (JToken result in results)
	        {
	            ParseNamedControl(result);
	        }
	    }

        /// <summary>
        /// Parses a single named control, and sets the values on the control
        /// </summary>
        /// <param name="result"></param>
	    private void ParseNamedControl(JToken result)
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
			settings.Port = m_Port == null ? (int?)null : m_Port.Id;
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

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
			}

			SetPort(port);

			// Load the config
			if (!string.IsNullOrEmpty(settings.Config))
				LoadControls(settings.Config);
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

			addRow("Connected", IsConnected);
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
	        yield return new ConsoleCommand("GetComponents", "Gets Components in Design", () => SendData(new ComponentGetComponentsRpc().Serialize()));
			yield return new ConsoleCommand("ReloadControls", "Reload controls from previous file" ,() => ReloadControls());
			yield return new GenericConsoleCommand<string>("LoadControls", "Load Controls from Specified File", p => LoadControls(p));

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

			yield return ConsoleNodeGroup.KeyNodeMap("NamedControls", GetNamedControls(), c => (uint)c.Id);
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
    }
}
