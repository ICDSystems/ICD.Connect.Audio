namespace ICD.Connect.Audio.Shure.Devices.MXW
{
	public sealed class ShureMxwApt4Device : AbstractShureMxwAptDevice<ShureMxwApt4DeviceSettings>
	{
		private const int NUM_CHANNELS = 4;

		protected override int NumberOfChannels { get { return NUM_CHANNELS; } }
	}
}
