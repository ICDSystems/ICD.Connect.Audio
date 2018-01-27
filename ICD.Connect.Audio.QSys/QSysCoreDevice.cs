using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using ICD.Connect.Audio.QSys.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;
using ICD.Connect.Devices;
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


        // RPCID's are used to seperate responses from the QSys based on the command sent
	    internal const string RPCID_NO_OP = "NoOp";
	    internal const string RPCID_GET_STATUS = "Status";
	    internal const string RPCID_NAMED_CONTROL = "NamedControl";
	    internal const string RPCID_NAMED_COMPONENT = "NamedComponent";
	    internal const string RPCID_CHANGEGROUP_RESPONSE = "ChangeGroupResponse";

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

	    private Dictionary<string, INamedControl> m_NamedControls;
	    private SafeCriticalSection m_NamedControlsCriticalSection;

        private readonly ISerialBuffer m_SerialBuffer;

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

            Heartbeat.StartMonitoring();

            m_NamedControlsCriticalSection = new SafeCriticalSection();
            m_NamedControls = new Dictionary<string, INamedControl>();
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

	    public void AddNamedControl(INamedControl namedControl)
	    {
	        m_NamedControlsCriticalSection.Enter();

	        try
	        {
	            m_NamedControls.Add(namedControl.ControlName, namedControl);
	        }
	        finally
	        {
	            m_NamedControlsCriticalSection.Leave();
	        }
	    }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Sends the data to the device and calls the callback asynchronously with the response.
        /// </summary>
        /// <param name="json"></param>
        internal void SendData(string json)
		{
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
			JsonUtils.Print(stringEventArgs.Data);

		    JObject json = JObject.Parse(stringEventArgs.Data);

            string responseId = (string)json.SelectToken("id");

		    switch (responseId)
		    {
                case (RPCID_NO_OP):
                    return;
                case (RPCID_NAMED_CONTROL):
                    ParseNamedControls(json);
                    break;

		    }
		}

        /// <summary>
        /// Parses one or more Named Controls, and sets the values on the controls
        /// </summary>
        /// <param name="json"></param>
	    private void ParseNamedControls(JObject json)
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

            string valueString = (string)result.SelectToken("String");
	        float valueValue = (float)result.SelectToken("Value");
            float valuePostion = (float)result.SelectToken("Position");

	        control.SetFeedback(valueString, valueValue, valuePostion);

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

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
			}

			SetPort(port);
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
	        yield return new GenericConsoleCommand<string, string, string>("SetComponent", "SetComponent <Component Name> <Control Name> <Control Value>", (p1,p2,p3) => SendData(new ComponentSetRpc(p1, p2, p3).Serialize()));
	        yield return new GenericConsoleCommand<string>("AddNamedControl", "AddNamedControl <Control Name>", p => AddNamedControl(new NamedControl(this, p)));
            yield return new GenericConsoleCommand<string>("AddBooleanNamedControl", "AddBooleanNamedControl <Control Name>", p => AddNamedControl(new BooleanNamedControl(this, p)));

        }

        /// <summary>
        /// Workaround for "unverifiable code" warning.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
	    {
	        return base.GetConsoleCommands();
	    }

        #endregion
    }
}
