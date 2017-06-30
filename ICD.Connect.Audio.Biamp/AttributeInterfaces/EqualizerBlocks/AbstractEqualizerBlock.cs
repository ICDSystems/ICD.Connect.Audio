namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.EqualizerBlocks
{
	public abstract class AbstractEqualizerBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractEqualizerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
