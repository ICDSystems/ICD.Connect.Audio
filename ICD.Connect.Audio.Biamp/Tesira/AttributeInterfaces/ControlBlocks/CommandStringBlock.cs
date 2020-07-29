namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.ControlBlocks
{
	public sealed class CommandStringBlock : AbstractControlBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public CommandStringBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
