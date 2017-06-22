namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks
{
	public sealed class LogicInputBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public LogicInputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
