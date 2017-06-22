namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
	public sealed class AutoMixerCombinerBlock : AbstractMixerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public AutoMixerCombinerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
