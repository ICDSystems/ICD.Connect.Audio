namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks
{
	public sealed class LogicMeterBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public LogicMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
