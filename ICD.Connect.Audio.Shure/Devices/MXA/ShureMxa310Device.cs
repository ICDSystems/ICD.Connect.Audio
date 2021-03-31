namespace ICD.Connect.Audio.Shure.Devices.MXA
{
	public sealed class ShureMxa310Device : AbstractShureMxaDevice<ShureMxa310DeviceSettings>
	{
		/// <summary>
		/// Gets the number of channels supported by the microphone.
		/// </summary>
		protected override int NumberOfChannels { get { return 4; } }
	}
}
