namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks
{
	public sealed class FlipFlopBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public FlipFlopBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
