namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.DynamicBlocks
{
	public sealed class CompressorBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public CompressorBlock(BiampTesiraDevice device, string instanceTag) : base(device, instanceTag)
		{
		}
	}
}
