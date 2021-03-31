namespace ICD.Connect.Audio.Shure.Devices.MXA
{
	public sealed class ShureMxa910Device : AbstractShureMxaDevice<ShureMxa910DeviceSettings>
	{
		/// <summary>
		/// Gets the number of channels supported by the microphone.
		/// </summary>
		protected override int NumberOfChannels { get { return 8; } }
	}
}
