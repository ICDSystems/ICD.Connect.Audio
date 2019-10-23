using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Shure.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Shure.Devices.MX
{
	public sealed class ShureMx396Device : AbstractDevice<ShureMx396DeviceSettings>
	{
		/// <summary>
		/// Raised when the microphone button is pressed.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnButtonPressedChanged;

		/// <summary>
		/// Raised when the LED state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnLedStateChanged;

		private IDigitalInputPort m_ButtonPort;
		private IRelayPort m_LedStatePort;

		private bool m_ButtonPressed;
		private bool m_LedState;

		#region Properties

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
		/// Gets the enabled state of the green LED.
		/// </summary>
		[PublicAPI]
		public bool LedState
		{
			get { return m_LedState; }
			private set
			{
				if (value == m_LedState)
					return;

				m_LedState = value;

				Log(eSeverity.Informational, "{0} - LED state changed to {1}", this, m_LedState);

				OnLedStateChanged.Raise(this, new BoolEventArgs(m_LedState));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShureMx396Device()
		{
			Controls.Add(new ShureMicRouteSourceControl(this, 0));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnButtonPressedChanged = null;
			OnLedStateChanged = null;

			base.DisposeFinal(disposing);

			SetPorts(null, null);
		}

		#region Methods

		/// <summary>
		/// Sets the ports used for communication with the microphone hardware.
		/// </summary>
		/// <param name="buttonPort"></param>
		/// <param name="ledStatePort"></param>
		[PublicAPI]
		public void SetPorts(IDigitalInputPort buttonPort, IRelayPort ledStatePort)
		{
			Unsubscribe(m_ButtonPort);
			Unsubscribe(m_LedStatePort);

			m_ButtonPort = buttonPort;
			m_LedStatePort = ledStatePort;

			Subscribe(m_ButtonPort);
			Subscribe(m_LedStatePort);

			ButtonPressed = m_ButtonPort != null && m_ButtonPort.State;
			LedState = m_LedStatePort != null && m_LedStatePort.Closed;

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Sets the LED to green when true, otherwise red.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public void SetLedState(bool enabled)
		{
			if (m_LedStatePort != null)
				m_LedStatePort.SetClosed(enabled);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ButtonPort != null &&
			       m_ButtonPort.IsOnline &&
			       m_LedStatePort != null &&
			       m_LedStatePort.IsOnline;
		}

		#endregion

		#region Button Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="buttonPort"></param>
		private void Subscribe(IDigitalInputPort buttonPort)
		{
			if (buttonPort == null)
				return;

			buttonPort.OnIsOnlineStateChanged += ButtonPortOnIsOnlineStateChanged;
			buttonPort.OnStateChanged += ButtonPortOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="buttonPort"></param>
		private void Unsubscribe(IDigitalInputPort buttonPort)
		{
			if (buttonPort == null)
				return;

			buttonPort.OnIsOnlineStateChanged -= ButtonPortOnIsOnlineStateChanged;
			buttonPort.OnStateChanged -= ButtonPortOnStateChanged;
		}

		/// <summary>
		/// Called when the button port state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ButtonPortOnStateChanged(object sender, BoolEventArgs eventArgs)
		{
			ButtonPressed = eventArgs.Data;
		}

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ButtonPortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region LED Port Callbacks

		/// <summary>
		/// Subscribe to the LED state port events.
		/// </summary>
		/// <param name="ledStatePort"></param>
		private void Subscribe(IRelayPort ledStatePort)
		{
			if (ledStatePort == null)
				return;

			ledStatePort.OnIsOnlineStateChanged += LedStatePortOnIsOnlineStateChanged;
			ledStatePort.OnClosedStateChanged += LedStatePortOnClosedStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the LED state port events.
		/// </summary>
		/// <param name="ledStatePort"></param>
		private void Unsubscribe(IRelayPort ledStatePort)
		{
			if (ledStatePort == null)
				return;

			ledStatePort.OnIsOnlineStateChanged -= LedStatePortOnIsOnlineStateChanged;
			ledStatePort.OnClosedStateChanged -= LedStatePortOnClosedStateChanged;
		}

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void LedStatePortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when the button port state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void LedStatePortOnClosedStateChanged(object sender, BoolEventArgs eventArgs)
		{
			LedState = eventArgs.Data;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(ShureMx396DeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.ButtonInputPort = m_ButtonPort == null ? (int?)null : m_ButtonPort.Id;
			settings.LedStatePort = m_LedStatePort == null ? (int?)null : m_LedStatePort.Id;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPorts(null, null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(ShureMx396DeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			IDigitalInputPort buttonInputPort = GetPortFromSettings<IDigitalInputPort>(factory, settings.ButtonInputPort);
			IRelayPort ledStatePort = GetPortFromSettings<IRelayPort>(factory, settings.LedStatePort);

			SetPorts(buttonInputPort, ledStatePort);
		}

		private TPort GetPortFromSettings<TPort>(IDeviceFactory factory, int? portId)
			where TPort : class, IPort
		{
			if (portId == null)
				return null;

			TPort port = factory.GetPortById((int)portId) as TPort;
			if (port == null)
				Log(eSeverity.Error, "No IO Port with id {0}", portId);

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
			addRow("Led State", LedState);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetLedState", "SetLedState <true/false>", a => SetLedState(a));
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
