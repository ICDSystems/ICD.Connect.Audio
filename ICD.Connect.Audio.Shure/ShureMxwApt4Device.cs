using System.Linq;

namespace ICD.Connect.Audio.Shure
{
	public class ShureMxwApt4Device : AbstractShureMicDevice<ShureMxwApt4DeviceSettings>, IShureMicDevice
	{
		private const int NUM_CHANNELS = 4;

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
			foreach (int channel in Enumerable.Range(1, NUM_CHANNELS))
				SetLedColor(color, channel);
		}

		private void SetLedColor(eLedColor color, int channel)
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
				Channel = channel,
				Command = "LED_STATUS",
				Value = leds
			};

			Send(command.Serialize());
		}

		private void SetLedOff()
		{
			foreach (int channel in Enumerable.Range(1, NUM_CHANNELS))
				SetLedOff(channel);
		}

		private void SetLedOff(int channel)
		{
			ShureMicSerialData command = new ShureMicSerialData
			{
				Type = ShureMicSerialData.SET,
				Channel = channel,
				Command = "LED_STATUS",
				Value = "OF OF"
			};
		}

		#endregion
	}
}
