namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
	public abstract class AbstractMixerBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		protected AbstractMixerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
