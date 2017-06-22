namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks
{
	public sealed class CommandStringBlock : AbstractControlBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public CommandStringBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
