namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.DynamicBlocks
{
	public sealed class NoiseGateBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public NoiseGateBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
