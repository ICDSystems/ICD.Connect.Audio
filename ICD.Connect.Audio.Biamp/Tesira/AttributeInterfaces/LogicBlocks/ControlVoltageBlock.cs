namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks
{
	public sealed class ControlVoltageBlock : AbstractLogicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public ControlVoltageBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
