namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MeterBlocks
{
	public sealed class SignalPresentMeterBlock : AbstractMeterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public SignalPresentMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
