using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Misc.BiColorMicButton
{
	public sealed class BiColorMicButtonDevice : AbstractBiColorMicButtonDevice<IIoPort, BiColorMicButtonDeviceSettings>
	{
		#region Methods

		/// <summary>
		/// Turns on/off the controller power.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetPowerEnabled(bool enabled)
		{
			if (PortPower != null)
				PortPower.SetDigitalOut(enabled);
		}

		/// <summary>
		/// Turns on/off the ring of red LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetRedLedEnabled(bool enabled)
		{
			if (PortRedLed != null)
				PortRedLed.SetDigitalOut(enabled);
		}

		/// <summary>
		/// Turns on/off the ring of green LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetGreenLedEnabled(bool enabled)
		{
			if (PortGreenLed != null)
				PortGreenLed.SetDigitalOut(enabled);
		}

		#endregion

		#region Port Callbacks

		private void SubscribeOutputPort(IIoPort port)
		{
			if (port == null)
				return;

			Subscribe(port);
			port.OnConfigurationChanged += PortOnConfigurationChanged;
			port.OnDigitalOutChanged += PortOnDigitalOutChanged;
		}

		private void UnsubscribeOutputPort(IIoPort port)
		{
			if (port == null)
				return;

			Unsubscribe(port);
			port.OnConfigurationChanged -= PortOnConfigurationChanged;
			port.OnDigitalOutChanged -= PortOnDigitalOutChanged;
		}

		private void ConfigureOuputPort(IIoPort port)
		{
			if (port == null)
				return;

			if (port.Configuration != eIoPortConfiguration.DigitalOut)
				port.SetConfiguration(eIoPortConfiguration.DigitalOut);
		}

		private void PortOnConfigurationChanged(IIoPort port, eIoPortConfiguration configuration)
		{
			ConfigureOuputPort(port);
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

			if (sender == PortPower)
				PowerEnabled = args.Data;

			if (sender == PortRedLed)
				RedLedEnabled = args.Data;

			if (sender == PortGreenLed)
				GreenLedEnabled = args.Data;
		}

		#region Port Power

		protected override void SubscribePortPower(IIoPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortPower(IIoPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void ConfigurePortPower(IIoPort port)
		{
			ConfigureOuputPort(port);
		}

		protected override void UpdatePowerState()
		{
			PowerEnabled = PortPower != null && PortPower.State;
		}

		#endregion

		#region Port Red Led

		protected override void SubscribePortRedLed(IIoPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortRedLed(IIoPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void ConfigurePortRedLed(IIoPort port)
		{
			ConfigureOuputPort(port);
		}

		protected override void UpdateRedLedState()
		{
				RedLedEnabled = PortRedLed != null && PortRedLed.State;
		}

		#endregion

		#region Green LED

		protected override void SubscribePortGreenLed(IIoPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortGreenLed(IIoPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void ConfigurePortGreenLed(IIoPort port)
		{
			ConfigureOuputPort(port);
		}

		protected override void UpdateGreenLedState()
		{
			GreenLedEnabled = PortGreenLed != null && PortGreenLed.State;
		}

		#endregion

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(BiColorMicButtonDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.PowerOutputPort = PortPower == null ? (int?)null : PortPower.Id;
			settings.RedLedOutputPort = PortRedLed == null ? (int?)null : PortRedLed.Id;
			settings.GreenLedOutputPort = PortGreenLed == null ? (int?)null : PortGreenLed.Id;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(BiColorMicButtonDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			PortPower = GetPortFromSettings<IIoPort>(factory, settings.PowerOutputPort);
			PortRedLed = GetPortFromSettings<IIoPort>(factory, settings.RedLedOutputPort);
			PortGreenLed = GetPortFromSettings<IIoPort>(factory, settings.GreenLedOutputPort);
		}

		#endregion
	}
}
