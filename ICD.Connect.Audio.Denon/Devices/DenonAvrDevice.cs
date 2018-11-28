using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Denon.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Denon.Devices
{
	public sealed class DenonAvrDevice : AbstractDevice<DenonAvrDeviceSettings>
	{
		public delegate void ResponseCallback(DenonAvrDevice device, DenonSerialData response);

		private static readonly ComSpec s_DefaultComSpec = new ComSpec
		{
			BaudRate = eComBaudRates.ComspecBaudRate9600,
			NumberOfDataBits = eComDataBits.ComspecDataBits8,
			ParityType = eComParityType.ComspecParityNone,
			NumberOfStopBits = eComStopBits.ComspecStopBits1,
			ProtocolType = eComProtocolType.ComspecProtocolRS232,
			HardwareHandShake = eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
			SoftwareHandshake = eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
			ReportCtsChanges = false
		};

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the device becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Raised when a response is received from the device.
		/// </summary>
		public event ResponseCallback OnDataReceived; 

		private readonly ISerialBuffer m_SerialBuffer;
		private readonly ConnectionStateManager m_ConnectionStateManager;

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
			m_SerialBuffer = new DelimiterSerialBuffer(DenonSerialData.DELIMITER);
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			Controls.Add(new DenonAvrSwitcherRoutingControl(this, 0));
			Controls.Add(new DenonAvrPowerControl(this, 1));
			Controls.Add(new DenonAvrVolumeControl(this, 2));
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;
			OnDataReceived = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_SerialBuffer);
		}

		/// <summary>
		/// Connect to the device.
		/// </summary>
		[PublicAPI]
		public void Connect()
		{
			m_ConnectionStateManager.Connect();
		}

		/// <summary>
		/// Disconnect from the device.
		/// </summary>
		[PublicAPI]
		public void Disconnect()
		{
			m_ConnectionStateManager.Disconnect();
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

		public void ConfigurePort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);
		}

		/// <summary>
		/// Configures a com port for communication with the hardware.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(s_DefaultComSpec);
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

			m_ConnectionStateManager.Send(data.Serialize());
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
		/// Initialize the device.
		/// </summary>
		private void Initialize()
		{
			Initialized = true;
		}

		#endregion

		#region Port Callbacks

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
				m_SerialBuffer.Clear();

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

		private void PortOnSerialDataReceived(object sender, StringEventArgs args)
		{
			m_SerialBuffer.Enqueue(args.Data);
		}

		#endregion

		#region Serial Buffer Callbacks

		/// <summary>
		/// Subscribes to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferOnOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribes from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferOnOnCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a response from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void BufferOnOnCompletedSerial(object sender, StringEventArgs args)
		{
			ResponseCallback handler = OnDataReceived;
			if (handler != null)
				handler(this, new DenonSerialData(args.Data));
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

			settings.Port = m_ConnectionStateManager.PortNumber;
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
