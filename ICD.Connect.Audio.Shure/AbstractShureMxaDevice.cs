using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Shure
{
	public abstract class AbstractShureMxaDevice<TSettings> : AbstractDevice<TSettings>, IShureMxaDevice
		where TSettings : AbstractShureMxaDeviceSettings, new()
	{
		private readonly ConnectionStateManager m_ConnectionStateManager;

		protected AbstractShureMxaDevice()
		{
			m_ConnectionStateManager = new ConnectionStateManager(this);
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.Dispose();
		}

		#region Methods

		/// <summary>
		/// Sets the brightness of the hardware LED.
		/// </summary>
		/// <param name="brightness"></param>
		public void SetLedBrightness(eLedBrightness brightness)
		{
			Send(string.Format("< SET LED_BRIGHTNESS {0} >", (int)brightness));
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is muted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedMuteColor(eLedColor color)
		{
			Send(string.Format("< SET LED_COLOR_MUTED {0} >", color.ToString().ToUpper()));
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is unmuted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedUnmuteColor(eLedColor color)
		{
			Send(string.Format("< SET LED_COLOR_UNMUTED {0} >", color.ToString().ToUpper()));
		}

		/// <summary>
		/// Sets the color of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedColor(eLedColor color)
		{
			SetLedMuteColor(color);
			SetLedUnmuteColor(color);
		}

		/// <summary>
		/// Enables/disables LED flashing.
		/// </summary>
		/// <param name="on"></param>
		public void SetLedFlash(bool on)
		{
			Send(string.Format("< SET FLASH {0} >", on ? "ON" : "OFF"));
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsConnected;
		}

		#endregion

		/// <summary>
		/// Sends the message to the device.
		/// </summary>
		/// <param name="message"></param>
		private void Send(string message)
		{
			m_ConnectionStateManager.Send(message + "\r\n");
		}

		#region Port Callbacks

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
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

			settings.Port = m_ConnectionStateManager.PortNumber;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_ConnectionStateManager.SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Logger.AddEntry(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
			}

			m_ConnectionStateManager.SetPort(port);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string setLedBrightnessHelp =
				string.Format("SetLedBrightness <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eLedBrightness>()));

			yield return new GenericConsoleCommand<eLedBrightness>("SetLedBrightness", setLedBrightnessHelp, e => SetLedBrightness(e));

			string colorEnumString = string.Format("<{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eLedColor>()));

			yield return new GenericConsoleCommand<eLedColor>("SetLedColor", "SetLedColor " + colorEnumString, e => SetLedColor(e));
			yield return new GenericConsoleCommand<eLedColor>("SetLedMuteColor", "SetLedMuteColor " + colorEnumString, e => SetLedMuteColor(e));
			yield return new GenericConsoleCommand<eLedColor>("SetLedUnmuteColor", "SetLedUnmuteColor " + colorEnumString, e => SetLedUnmuteColor(e));
			yield return new GenericConsoleCommand<bool>("SetLedFlash", "SetLedFlash <true/false>", o => SetLedFlash(o));
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
