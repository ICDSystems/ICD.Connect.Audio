namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks
{
	public sealed class LogicDelayBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public LogicDelayBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
