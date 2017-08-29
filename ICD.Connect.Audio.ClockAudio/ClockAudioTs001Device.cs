using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.ClockAudio
{
	public sealed class ClockAudioTs001Device : AbstractDevice<ClockAudioTs001DeviceSettings>
	{
		private const int BUTTON_INDEX = 0;
		private const int RED_LED_INDEX = 1;
		private const int GREEN_LED_INDEX = 2;
		private const int VOLTAGE_INDEX = 3;

		/// <summary>
		/// Raised when the microphone button is pressed.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnButtonPressedChanged;

		/// <summary>
		/// Raised when the red LED enabled state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnRedLedEnabledChanged;

		/// <summary>
		/// Raised when the green LED enabled state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnGreenLedEnabledChanged;

		/// <summary>
		/// Raised when the microphone voltage changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<UShortEventArgs> OnVoltageChanged; 

		private readonly IIoPort[] m_Ports;
		private readonly SafeCriticalSection m_PortsSection;

		private ushort m_Voltage;
		private bool m_ButtonPressed;
		private bool m_GreenLedEnabled;
		private bool m_RedLedEnabled;

		#region Properties

		/// <summary>
		/// Gets the voltage reported by the microphone hardware.
		/// </summary>
		[PublicAPI]
		public ushort Voltage
		{
			get { return m_Voltage; }
			private set
			{
				if (value == m_Voltage)
					return;

				m_Voltage = value;

				OnVoltageChanged.Raise(this, new UShortEventArgs(m_Voltage));
			}
		}

		/// <summary>
		/// Gets the current presses state of the button.
		/// </summary>
		[PublicAPI]
		public bool ButtonPressed
		{
			get { return m_ButtonPressed; }
			private set
			{
				if (value == m_ButtonPressed)
					return;

				m_ButtonPressed = value;

				OnButtonPressedChanged.Raise(this, new BoolEventArgs(m_ButtonPressed));
			}
		}

		/// <summary>
		/// Gets the enabled state of the red LED.
		/// </summary>
		[PublicAPI]
		public bool RedLedEnabled
		{
			get { return m_RedLedEnabled; }
			private set
			{
				if (value == m_RedLedEnabled)
					return;

				m_RedLedEnabled = value;

				OnRedLedEnabledChanged.Raise(this, new BoolEventArgs(m_RedLedEnabled));
			}
		}

		/// <summary>
		/// Gets the enabled state of the green LED.
		/// </summary>
		[PublicAPI]
		public bool GreenLedEnabled
		{
			get { return m_GreenLedEnabled; }
			private set
			{
				if (value == m_GreenLedEnabled)
					return;

				m_GreenLedEnabled = value;

				OnGreenLedEnabledChanged.Raise(this, new BoolEventArgs(m_GreenLedEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ClockAudioTs001Device()
		{
			m_Ports = new IIoPort[4];
			m_PortsSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnButtonPressedChanged = null;
			OnVoltageChanged = null;
			OnRedLedEnabledChanged = null;
			OnGreenLedEnabledChanged = null;

			base.DisposeFinal(disposing);

			SetPorts(null, null, null, null);
		}

		#region Methods

		/// <summary>
		/// Sets the ports used for communication with the microphone hardware.
		/// </summary>
		/// <param name="buttonInput"></param>
		/// <param name="redLedOutput"></param>
		/// <param name="greenLedOutput"></param>
		/// <param name="voltageInput"></param>
		[PublicAPI]
		public void SetPorts(IIoPort buttonInput, IIoPort redLedOutput, IIoPort greenLedOutput, IIoPort voltageInput)
		{
			m_PortsSection.Enter();

			try
			{
				foreach (IIoPort port in m_Ports)
					Unsubscribe(port);

				m_Ports[BUTTON_INDEX] = buttonInput;
				m_Ports[RED_LED_INDEX] = redLedOutput;
				m_Ports[GREEN_LED_INDEX] = greenLedOutput;
				m_Ports[VOLTAGE_INDEX] = voltageInput;

				foreach (IIoPort port in m_Ports)
					Subscribe(port);

				UpdatePortConfigurations();

				ButtonPressed = buttonInput != null && buttonInput.DigitalIn;
				Voltage = voltageInput == null ? (ushort)0 : voltageInput.AnalogIn;
				RedLedEnabled = redLedOutput != null && redLedOutput.DigitalOut;
				GreenLedEnabled = greenLedOutput != null && greenLedOutput.DigitalOut;
			}
			finally
			{
				m_PortsSection.Leave();
			}
		}

		/// <summary>
		/// Turns on/off the ring of red LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public void SetRedLedEnabled(bool enabled)
		{
			IIoPort port = m_PortsSection.Execute(() => m_Ports[RED_LED_INDEX]);
			if (port != null)
				port.SetDigitalOut(enabled);
		}

		/// <summary>
		/// Turns on/off the ring of green LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public void SetGreenLedEnabled(bool enabled)
		{
			IIoPort port = m_PortsSection.Execute(() => m_Ports[GREEN_LED_INDEX]);
			if (port != null)
				port.SetDigitalOut(enabled);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			// Can be called before constructor
			if (m_PortsSection == null || m_Ports == null)
				return false;

			m_PortsSection.Enter();

			try
			{
				// Returns false if there are no ports, or any of the available ports are offline.
				return !m_Ports.Any(port => port != null && !port.IsOnline) && m_Ports.Any(p => p != null);
			}
			finally
			{
				m_PortsSection.Leave();
			}
		}

		/// <summary>
		/// Forces the ports to have the correct configurations to communicate properly with the microphone.
		/// </summary>
		private void UpdatePortConfigurations()
		{
			m_PortsSection.Enter();

			try
			{
				if (m_Ports[BUTTON_INDEX] != null)
					m_Ports[BUTTON_INDEX].SetConfiguration(eIoPortConfiguration.DigitalIn);

				if (m_Ports[RED_LED_INDEX] != null)
					m_Ports[RED_LED_INDEX].SetConfiguration(eIoPortConfiguration.DigitalOut);

				if (m_Ports[GREEN_LED_INDEX] != null)
					m_Ports[GREEN_LED_INDEX].SetConfiguration(eIoPortConfiguration.DigitalOut);

				if (m_Ports[VOLTAGE_INDEX] != null)
					m_Ports[VOLTAGE_INDEX].SetConfiguration(eIoPortConfiguration.AnalogIn);
			}
			finally
			{
				m_PortsSection.Leave();
			}
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(IIoPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			port.OnAnalogInChanged += PortOnAnalogInChanged;
			port.OnDigitalInChanged += PortOnDigitalInChanged;
			port.OnDigitalOutChanged += PortOnDigitalOutChanged;
			port.OnConfigurationChanged += PortOnConfigurationChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(IIoPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			port.OnAnalogInChanged -= PortOnAnalogInChanged;
			port.OnDigitalInChanged -= PortOnDigitalInChanged;
			port.OnDigitalOutChanged += PortOnDigitalOutChanged;
			port.OnConfigurationChanged -= PortOnConfigurationChanged;
		}

		/// <summary>
		/// Called when we get a configuration change event from one of the ports.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="configuration"></param>
		private void PortOnConfigurationChanged(IIoPort port, eIoPortConfiguration configuration)
		{
			UpdatePortConfigurations();
		}

		/// <summary>
		/// Called when we get an online state change from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when we get a digital input signal from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnDigitalInChanged(object sender, BoolEventArgs args)
		{
			if (sender != null && sender == m_PortsSection.Execute(() => m_Ports[BUTTON_INDEX]))
				ButtonPressed = args.Data;
		}

		/// <summary>
		/// Called when the digital out signal for a port changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnDigitalOutChanged(object sender, BoolEventArgs args)
		{
			if (sender == null)
				return;

			if (sender == m_PortsSection.Execute(() => m_Ports[RED_LED_INDEX]))
				RedLedEnabled = args.Data;

			if (sender == m_PortsSection.Execute(() => m_Ports[GREEN_LED_INDEX]))
				GreenLedEnabled = args.Data;
		}

		/// <summary>
		/// Called when we get an analog input signal from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnAnalogInChanged(object sender, UShortEventArgs args)
		{
			if (sender != null && sender == m_PortsSection.Execute(() => m_Ports[VOLTAGE_INDEX]))
				Voltage = args.Data;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(ClockAudioTs001DeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			m_PortsSection.Enter();

			try
			{
				settings.ButtonInputPort = m_Ports[BUTTON_INDEX] == null ? (int?)null : m_Ports[BUTTON_INDEX].Id;
				settings.VoltageInputPort = m_Ports[VOLTAGE_INDEX] == null ? (int?)null : m_Ports[VOLTAGE_INDEX].Id;
				settings.RedLedOutputPort = m_Ports[RED_LED_INDEX] == null ? (int?)null : m_Ports[RED_LED_INDEX].Id;
				settings.GreenLedOutputPort = m_Ports[GREEN_LED_INDEX] == null ? (int?)null : m_Ports[GREEN_LED_INDEX].Id;
			}
			finally
			{
				m_PortsSection.Leave();
			}
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPorts(null, null, null, null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(ClockAudioTs001DeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			IIoPort buttonInputPort = GetPortFromSettings(factory, settings.ButtonInputPort);
			IIoPort voltageInputPort = GetPortFromSettings(factory, settings.VoltageInputPort);
			IIoPort redLedInputPort = GetPortFromSettings(factory, settings.RedLedOutputPort);
			IIoPort greenLedInputPort = GetPortFromSettings(factory, settings.GreenLedOutputPort);

			SetPorts(buttonInputPort, redLedInputPort, greenLedInputPort, voltageInputPort);
		}

		private IIoPort GetPortFromSettings(IDeviceFactory factory, int? portId)
		{
			if (portId == null)
				return null;

			IIoPort port = factory.GetPortById((int)portId) as IIoPort;
			if (port == null)
				Logger.AddEntry(eSeverity.Error, "No Serial Port with id {0}", portId);

			return port;
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

			addRow("Button Pressed", ButtonPressed);
			addRow("Voltage", Voltage);
			addRow("Red Led Enabled", RedLedEnabled);
			addRow("Green Led Enabled", GreenLedEnabled);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetRedLedEnabled", "SetRedLedEnabled <true/false>", a => SetRedLedEnabled(a));
			yield return new GenericConsoleCommand<bool>("SetGreenLedEnabled", "SetGreenLedEnabled <true/false>", a => SetGreenLedEnabled(a));
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
