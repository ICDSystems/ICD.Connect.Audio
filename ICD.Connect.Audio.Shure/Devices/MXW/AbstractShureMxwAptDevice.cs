using System.Collections.Generic;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Audio.Shure.Devices.MXW
{
	public abstract class AbstractShureMxwAptDevice<TSettings> : AbstractShureMicDevice<TSettings>
		where TSettings : AbstractShureMxwAptDeviceSettings, new()
	{
		public override void SetLedStatus(eLedColor color, eLedBrightness brightness)
		{
			if (brightness == eLedBrightness.Disabled)
				SetLedOff();
			else
				SetLedColor(color);
		}

		#region Private Methods

		private void SetLedColor(eLedColor color)
		{
			string leds = "OF OF";
			switch (color)
			{
				case eLedColor.Green:
					leds = "OF ON";
					break;
				case eLedColor.Red:
					leds = "ON OF";
					break;
				case eLedColor.Yellow:
					leds = "ON ON";
					break;
			}

			ShureMicSerialData command = new ShureMicSerialData
			{
				Type = ShureMicSerialData.SET,
				Channel = 0,
				Command = "LED_STATUS",
				Value = leds
			};

			Send(command.Serialize());
		}

		private void SetLedOff()
		{
			ShureMicSerialData command = new ShureMicSerialData
			{
				Type = ShureMicSerialData.SET,
				Channel = 0,
				Command = "LED_STATUS",
				Value = "OF OF"
			};

			Send(command.Serialize());
		}

		#endregion

		#region Console

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (var command in GetBaseConsoleCommands())
				yield return command;
			
			yield return new GenericConsoleCommand<eLedColor>("SetLedColor", "SetLedColor <Red, Green, Yellow>", e => SetLedColor(e));
			yield return new ConsoleCommand("SetLedOff", "SetLedOff", () => SetLedOff());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
