namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
	public sealed class MatrixMixerBlock : AbstractMixerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public MatrixMixerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
