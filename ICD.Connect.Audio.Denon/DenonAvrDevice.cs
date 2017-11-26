using System;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Audio.Denon.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Denon
{
	public sealed class DenonAvrDevice : AbstractDevice<DenonAvrDeviceSettings>
	{
		// How often to check the connection and reconnect if necessary.
		private const long CONNECTION_CHECK_MILLISECONDS = 30 * 1000;

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the device becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private readonly SafeTimer m_ConnectionTimer;

		private ISerialPort m_Port;
		private readonly SerialQueue m_SerialQueue;

		private bool m_IsConnected;
		private bool m_Initialized;

		#region Properties

		/// <summary>
		/// Device Initialized Status.
		/// </summary>
		[PublicAPI]
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
		/// Returns true when the device is connected.
		/// </summary>
		[PublicAPI]
		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				UpdateCachedOnlineStatus();

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public DenonAvrDevice()
		{
			m_ConnectionTimer = new SafeTimer(ConnectionTimerCallback, 0, CONNECTION_CHECK_MILLISECONDS);

			m_SerialQueue = new SerialQueue();
			m_SerialQueue.SetBuffer(new DelimiterSerialBuffer(DenonSerialData.DELIMITER));

			Subscribe(m_SerialQueue);
			Controls.Add(new DenonAvrPowerControl(this));
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			m_ConnectionTimer.Dispose();

			base.DisposeFinal(disposing);

			Unsubscribe(m_SerialQueue);
			Unsubscribe(m_Port);
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

			if (IsConnected)
				Initialize();
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
			m_SerialQueue.SetPort(m_Port);

			Subscribe(m_Port);

			if (m_Port != null)
				Connect();

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Configures a com port for communication with the hardware.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate9600,
								eComDataBits.ComspecDataBits8,
								eComParityType.ComspecParityNone,
								eComStopBits.ComspecStopBits1,
								eComProtocolType.ComspecProtocolRS232,
								eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
								eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
								false);
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Sends the data to the device.
		/// </summary>
		/// <param name="data"></param>
		internal void SendData(DenonSerialData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			SendData(data.Serialize());
		}

		/// <summary>
		/// Sends the data to the device.
		/// </summary>
		/// <param name="data"></param>
		private void SendData(string data)
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

			ISerialData serial = new SerialData(data);
			m_SerialQueue.Enqueue(serial, (a, b) => a.Serialize() == b.Serialize());
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
		/// Initialize the device.
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
			return string.Format("{0} - {1}", GetType().Name, log);
		}

		/// <summary>
		/// Called periodically to maintain connection to the device.
		/// </summary>
		private void ConnectionTimerCallback()
		{
			if (m_Port != null && !m_Port.IsConnected)
				Connect();
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
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			IsConnected = args.Data;

			if (IsConnected)
				Initialize();
			else
			{
				m_SerialQueue.Clear();

				Log(eSeverity.Critical, "Lost connection");
				Initialized = false;

				m_ConnectionTimer.Reset(CONNECTION_CHECK_MILLISECONDS, CONNECTION_CHECK_MILLISECONDS);
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

		#region Serial Queue Callbacks

		/// <summary>
		/// Subscribes to the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Subscribe(SerialQueue queue)
		{
			queue.OnSerialResponse += QueueOnSerialResponse;
			queue.OnTimeout += QueueOnTimeout;
		}

		/// <summary>
		/// Unsubscribes from the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Unsubscribe(SerialQueue queue)
		{
			queue.OnSerialResponse -= QueueOnSerialResponse;
			queue.OnTimeout += QueueOnTimeout;
		}

		/// <summary>
		/// Called when we receive a response from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void QueueOnSerialResponse(object sender, SerialResponseEventArgs eventArgs)
		{
			IcdConsole.PrintLine(eventArgs.Response);
		}

		/// <summary>
		/// Called when a command sent to the device did not receive a response soon enough.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void QueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Error, "Timeout - {0}", args.Data.Serialize());
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(DenonAvrDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_Port == null ? (int?)null : m_Port.Id;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(DenonAvrDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

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
	}
}
