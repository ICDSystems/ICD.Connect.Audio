namespace ICD.Connect.Audio.Shure.Devices.MXW
{
	public sealed class ShureMxwApt8Device : AbstractShureMxwAptDevice<ShureMxwApt8DeviceSettings>
	{
		private const int NUM_CHANNELS = 8;

		protected override int NumberOfChannels { get { return NUM_CHANNELS; } }
	}
}
