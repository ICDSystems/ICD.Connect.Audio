namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.ControlBlocks
{
	public abstract class AbstractControlBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractControlBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
