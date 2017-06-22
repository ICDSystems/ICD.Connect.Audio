namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MeterBlocks
{
	public sealed class SignalPresentMeterBlock : AbstractMeterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public SignalPresentMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
