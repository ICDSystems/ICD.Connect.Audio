using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Shure
{
	public abstract class AbstractShureMxaDevice<TSettings> : AbstractShureMicDevice<TSettings>, IShureMxaDevice
		where TSettings : AbstractShureMxaDeviceSettings, new()
	{
		#region Methods

		/// <summary>
		/// Sets the brightness of the hardware LED.
		/// </summary>
		/// <param name="brightness"></param>
		public void SetLedBrightness(eLedBrightness brightness)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_BRIGHTNESS",
				Value = ((int)brightness).ToString()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is muted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedMuteColor(eLedColor color)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_COLOR_MUTED",
				Value = color.ToString().ToUpper()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is unmuted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedUnmuteColor(eLedColor color)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_COLOR_UNMUTED",
				Value = color.ToString().ToUpper()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Turns Metering On.
		/// </summary>
		/// <param name="milliseconds"></param>
		public void TurnMeteringOn(uint milliseconds)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "METER_RATE",
				Value = milliseconds.ToString()
			};

			Send(command.Serialize());
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
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="brightness"></param>
		public override void SetLedStatus(eLedColor color, eLedBrightness brightness)
		{
			if (brightness == eLedBrightness.Disabled)
			{
				SetLedBrightness(brightness);
				SetLedColor(color);
			}
			else
			{
				SetLedColor(color);
				SetLedBrightness(brightness);
			}
		}

		/// <summary>
		/// Enables/disables LED flashing.
		/// </summary>
		/// <param name="on"></param>
		public void SetLedFlash(bool on)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "FLASH",
				Value = on ? "ON" : "OFF"
			};

			Send(command.Serialize());
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
			yield return new GenericConsoleCommand<uint>("TurnMeteringOn", "TurnMeteringOn <uint>", o => TurnMeteringOn(o));
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
