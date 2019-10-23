namespace ICD.Connect.Audio.Shure.Devices.MXW
{
	public sealed class ShureMxwApt2Device : AbstractShureMxwAptDevice<ShureMxwApt2DeviceSettings>
	{
		private const int NUM_CHANNELS = 2;

		protected override int NumberOfChannels { get { return NUM_CHANNELS; } }
	}
}
