namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.EqualizerBlocks
{
	public sealed class FeedbackSuppressorBlock : AbstractEqualizerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public FeedbackSuppressorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
