namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks
{
	public abstract class AbstractLogicBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractLogicBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
