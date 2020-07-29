namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DelayBlocks
{
	public abstract class AbstractDelayBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractDelayBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
