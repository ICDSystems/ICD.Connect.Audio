using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Misc.BiColorMicButton
{
	public sealed class BiColorRelayMicButtonDevice : AbstractBiColorMicButtonDevice<IRelayPort, BiColorRelayMicButtonDeviceSettings>
	{

		#region Methods

		/// <summary>
		/// Turns on/off the controller power.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetPowerEnabled(bool enabled)
		{
			if (PortPower == null)
				return;

			if (enabled)
				PortPower.Close();
			else
				PortPower.Open();
		}

		/// <summary>
		/// Turns on/off the ring of red LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetRedLedEnabled(bool enabled)
		{
			if (PortRedLed == null)
				return;

			if (enabled)
				PortRedLed.Close();
			else
				PortRedLed.Open();
		}

		/// <summary>
		/// Turns on/off the ring of green LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		public override void SetGreenLedEnabled(bool enabled)
		{
			if (PortGreenLed == null)
				return;

			if (enabled)
				PortGreenLed.Close();
			else
				PortGreenLed.Open();
		}

		#endregion

		#region Port Callbacks

		private void SubscribeOutputPort(IRelayPort port)
		{
			if (port == null)
				return;

			Subscribe(port);
			port.OnClosedStateChanged += PortOnClosedStateChanged;
		}

		private void UnsubscribeOutputPort(IRelayPort port)
		{
			if (port == null)
				return;

			Unsubscribe(port);
			port.OnClosedStateChanged -= PortOnClosedStateChanged;
		}

		private void PortOnClosedStateChanged(object sender, BoolEventArgs args)
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

		protected override void SubscribePortPower(IRelayPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortPower(IRelayPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void UpdatePowerState()
		{
			PowerEnabled = PortPower != null && PortPower.Closed;
		}

		#endregion

		#region Port Red Led

		protected override void SubscribePortRedLed(IRelayPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortRedLed(IRelayPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void UpdateRedLedState()
		{
			RedLedEnabled = PortRedLed != null && PortRedLed.Closed;
		}

		#endregion

		#region Green LED

		protected override void SubscribePortGreenLed(IRelayPort port)
		{
			SubscribeOutputPort(port);
		}

		protected override void UnsubscribePortGreenLed(IRelayPort port)
		{
			UnsubscribeOutputPort(port);
		}

		protected override void UpdateGreenLedState()
		{
			GreenLedEnabled = PortGreenLed != null && PortGreenLed.Closed;
		}

		#endregion

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(BiColorRelayMicButtonDeviceSettings settings)
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
		protected override void ApplySettingsFinal(BiColorRelayMicButtonDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			PortPower = GetPortFromSettings<IRelayPort>(factory, settings.PowerOutputPort);
			PortRedLed = GetPortFromSettings<IRelayPort>(factory, settings.RedLedOutputPort);
			PortGreenLed = GetPortFromSettings<IRelayPort>(factory, settings.GreenLedOutputPort);
		}

		#endregion
	}
}