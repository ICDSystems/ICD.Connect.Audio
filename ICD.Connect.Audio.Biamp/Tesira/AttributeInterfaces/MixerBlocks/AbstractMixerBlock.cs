namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MixerBlocks
{
	public abstract class AbstractMixerBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractMixerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
