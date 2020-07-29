namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DynamicBlocks
{
	public sealed class PeakLimiterBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public PeakLimiterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
