namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DynamicBlocks
{
	public abstract class AbstractDynamicBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractDynamicBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
