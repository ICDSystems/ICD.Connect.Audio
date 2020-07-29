namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks
{
	public sealed class LogicMeterBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public LogicMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
