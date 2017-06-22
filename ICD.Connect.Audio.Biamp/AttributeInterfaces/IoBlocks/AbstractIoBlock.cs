namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public abstract class AbstractIoBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		protected AbstractIoBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
