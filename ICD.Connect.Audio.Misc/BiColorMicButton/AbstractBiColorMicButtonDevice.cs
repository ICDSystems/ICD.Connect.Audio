using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Misc.BiColorMicButton
{
	public abstract class AbstractBiColorMicButtonDevice<TPortType,TSettings> : AbstractDevice<TSettings>, IBiColorMicButton where TPortType : class, IPort where TSettings : IBiColorMicButtonDeviceSettings, new()
	{
		/// <summary>
		/// Raised when the microphone button is pressed.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnButtonPressedChanged;

		/// <summary>
		/// Raised when the red LED enabled state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnPowerEnabledChanged;

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

		private IDigitalInputPort m_PortButton;
		private IIoPort m_PortVoltage;
		private TPortType m_PortPower;
		private TPortType m_PortRedLed;
		private TPortType m_PortGreenLed;

		private ushort m_Voltage;
		private bool m_ButtonPressed;
		private bool m_PowerEnabled;
		private bool m_RedLedEnabled;
		private bool m_GreenLedEnabled;

		#region Properties

		public IDigitalInputPort PortButton
		{
			get { return m_PortButton; }
			private set
			{
				if (m_PortButton == value)
					return;

				UnsubscribePortButton(m_PortButton);
				m_PortButton = value;
				SubscribePortButton(m_PortButton);
				ConfigurePortButton(m_PortButton);
				ButtonPressed = m_PortButton.State;

				UpdateCachedOnlineStatus();
			}
		}

		public IIoPort PortVoltage
		{
			get { return m_PortVoltage; }
			private set
			{
				if (m_PortVoltage == value)
					return;

				UnsubscribePortVoltage(m_PortVoltage);
				m_PortVoltage = value;
				SubscribePortVoltage(m_PortVoltage);
				ConfigurePortVoltage(m_PortVoltage);
				Voltage = m_PortVoltage.AnalogIn;

				UpdateCachedOnlineStatus();
			}
		}

		public TPortType PortPower
		{
			get { return m_PortPower; }
			protected set
			{
				if (m_PortPower == value)
					return;

				UnsubscribePortPower(m_PortPower);
				m_PortPower = value;
				SubscribePortPower(m_PortPower);
				ConfigurePortPower(m_PortPower);
				UpdatePowerState();

				UpdateCachedOnlineStatus();
			}
		}

		public TPortType PortRedLed
		{
			get { return m_PortRedLed; }
			protected set
			{
				if (m_PortRedLed == value)
					return;

				UnsubscribePortRedLed(m_PortRedLed);
				m_PortRedLed = value;
				SubscribePortRedLed(m_PortRedLed);
				ConfigurePortRedLed(m_PortRedLed);
				UpdateRedLedState();

				UpdateCachedOnlineStatus();
			}
		}

		public TPortType PortGreenLed
		{
			get { return m_PortGreenLed; }
			protected set
			{
				if (m_PortGreenLed == value)
					return;

				UnsubscribePortGreenLed(m_PortGreenLed);
				m_PortGreenLed = value;
				SubscribePortGreenLed(m_PortGreenLed);
				ConfigurePortGreenLed(m_PortGreenLed);
				UpdateGreenLedState();
			}
		}

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

				Log(eSeverity.Informational, "{0} - Voltage changed to {1}", this, m_Voltage);

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

				Log(eSeverity.Informational, "{0} - Button pressed changed to {1}", this, m_ButtonPressed);

				OnButtonPressedChanged.Raise(this, new BoolEventArgs(m_ButtonPressed));
			}
		}

		/// <summary>
		/// Gets the enabled state of the power output.
		/// </summary>
		[PublicAPI]
		public bool PowerEnabled
		{
			get { return m_PowerEnabled; }
			protected set
			{
				if (value == m_PowerEnabled)
					return;

				m_PowerEnabled = value;

				Log(eSeverity.Informational, "{0} - Power enabled changed to {1}", this, m_PowerEnabled);

				OnPowerEnabledChanged.Raise(this, new BoolEventArgs(m_PowerEnabled));
			}
		}

		/// <summary>
		/// Gets the enabled state of the Red LED.
		/// </summary>
		[PublicAPI]
		public bool RedLedEnabled
		{
			get { return m_RedLedEnabled; }
			protected set
			{
				if (value == m_RedLedEnabled)
					return;

				m_RedLedEnabled = value;

				Log(eSeverity.Informational, "{0} - Red LED enabled changed to {1}", this, m_RedLedEnabled);

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
			protected set
			{
				if (value == m_GreenLedEnabled)
					return;

				m_GreenLedEnabled = value;

				Log(eSeverity.Informational, "{0} - Green LED enabled changed to {1}", this, m_GreenLedEnabled);

				OnGreenLedEnabledChanged.Raise(this, new BoolEventArgs(m_GreenLedEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnButtonPressedChanged = null;
			OnVoltageChanged = null;
			OnPowerEnabledChanged = null;
			OnRedLedEnabledChanged = null;
			OnGreenLedEnabledChanged = null;

			base.DisposeFinal(disposing);

			PortButton = null;
			PortVoltage = null;
			PortPower = null;
			PortRedLed = null;
			PortGreenLed = null;
		}

		#region Methods

		/// <summary>
		/// Turns on/off the controller power.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public abstract void SetPowerEnabled(bool enabled);

		/// <summary>
		/// Turns on/off the ring of red LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public abstract void SetRedLedEnabled(bool enabled);

		/// <summary>
		/// Turns on/off the ring of green LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public abstract void SetGreenLedEnabled(bool enabled);

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			// If no ports are defined, we're offline
			if (PortButton == null && PortVoltage == null && PortPower == null && PortRedLed == null && PortGreenLed == null)
				return false;

			bool online = true;

			if (PortButton != null)
				online &= PortButton.IsOnline;
			if (PortVoltage != null)
				online &= PortVoltage.IsOnline;
			if (PortPower != null)
				online &= PortPower.IsOnline;
			if (PortRedLed != null)
				online &= PortRedLed.IsOnline;
			if (PortGreenLed != null)
				online &= PortGreenLed.IsOnline;


			return online;
		}

		#endregion

		#region Port Callbacks

		protected void Subscribe(IPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		protected void Unsubscribe(IPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		#region Port Button

		private void SubscribePortButton(IDigitalInputPort port)
		{
			if (port == null)
				return;

			Subscribe(port);

			port.OnStateChanged += ButtonPortOnStateChanged;

			IIoPort ioPort = port as IIoPort;
			if (ioPort != null)
				ioPort.OnConfigurationChanged += ButtonPortOnConfigurationChanged;
		}

		private void UnsubscribePortButton(IDigitalInputPort port)
		{
			if (port == null)
				return;

			Unsubscribe(port);

			port.OnStateChanged -= ButtonPortOnStateChanged;

			IIoPort ioPort = port as IIoPort;
			if (ioPort != null)
				ioPort.OnConfigurationChanged -= ButtonPortOnConfigurationChanged;
		}

		private void ButtonPortOnConfigurationChanged(IIoPort port, eIoPortConfiguration configuration)
		{
			ConfigurePortButton(port);
		}

		private void ConfigurePortButton(IDigitalInputPort port)
		{
			IIoPort ioPort = port as IIoPort;

			if (ioPort != null)
				ConfigurePortButton(ioPort);
		}

		private void ConfigurePortButton(IIoPort port)
		{
			if (port == null)
				return;

			if (port.Configuration != eIoPortConfiguration.DigitalIn)
				port.SetConfiguration(eIoPortConfiguration.DigitalIn);
		}

		#endregion

		#region Port Voltage

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void SubscribePortVoltage(IIoPort port)
		{
			if (port == null)
				return;

			Subscribe(port);

			port.OnAnalogInChanged += VoltagePortOnAnalogInChanged;
			port.OnConfigurationChanged += VoltagePortOnConfigurationChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void UnsubscribePortVoltage(IIoPort port)
		{
			if (port == null)
				return;

			Unsubscribe(port);

			port.OnAnalogInChanged -= VoltagePortOnAnalogInChanged;
			port.OnConfigurationChanged -= VoltagePortOnConfigurationChanged;
		}

		/// <summary>
		/// Called when we get a configuration change event from one of the ports.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="configuration"></param>
		private void VoltagePortOnConfigurationChanged(IIoPort port, eIoPortConfiguration configuration)
		{
			ConfigurePortVoltage(port);
		}

		private void ConfigurePortVoltage(IIoPort port)
		{
			if (port == null)
				return;

			if (port.Configuration != eIoPortConfiguration.AnalogIn)
				port.SetConfiguration(eIoPortConfiguration.AnalogIn);
		}

		#endregion

		#region Port Power

		protected abstract void SubscribePortPower(TPortType port);
		protected abstract void UnsubscribePortPower(TPortType port);
		protected abstract void UpdatePowerState();
		protected virtual void ConfigurePortPower(TPortType port){}

		#endregion

		#region Port RedLed

		protected abstract void SubscribePortRedLed(TPortType port);
		protected abstract void UnsubscribePortRedLed(TPortType port);
		protected abstract void UpdateRedLedState();
		protected virtual void ConfigurePortRedLed(TPortType port){}

		#endregion

#region Port GreenLed

		protected abstract void SubscribePortGreenLed(TPortType port);
		protected abstract void UnsubscribePortGreenLed(TPortType port);
		protected abstract void UpdateGreenLedState();
		protected virtual void ConfigurePortGreenLed(TPortType port){}

		#endregion

		/// <summary>
		/// Called when we get an online state change from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when we get a digital input signal from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ButtonPortOnStateChanged(object sender, BoolEventArgs args)
		{
			if (sender != null && sender == m_PortButton)
				ButtonPressed = args.Data;
		}

		/// <summary>
		/// Called when we get an analog input signal from one of the ports.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void VoltagePortOnAnalogInChanged(object sender, UShortEventArgs args)
		{
			if (sender != null && sender == m_PortVoltage)
				Voltage = args.Data;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

				settings.ButtonInputPort = m_PortButton == null ? (int?)null : m_PortButton.Id;
				settings.VoltageInputPort = m_PortVoltage == null ? (int?)null : m_PortVoltage.Id;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			PortButton = null;
			PortVoltage = null;
			PortPower = null;
			PortRedLed = null;
			PortGreenLed = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			PortButton = GetPortFromSettings<IDigitalInputPort>(factory, settings.ButtonInputPort);
			PortVoltage = GetPortFromSettings<IIoPort>(factory, settings.VoltageInputPort);
		}

		protected T GetPortFromSettings<T>(IDeviceFactory factory, int? portId) where T : class, IPort
		{
			if (portId == null)
				return null;

			T port = factory.GetPortById((int)portId) as T;
			if (port == null)
				Log(eSeverity.Error, "No {1} Port with id {0}", portId,typeof(T));

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
			addRow("Power Enabled", PowerEnabled);
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

			yield return new GenericConsoleCommand<bool>("SetPowerEnabled", "SetPowerEnabled <true/false>", a => SetPowerEnabled(a));
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