namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.ControlBlocks
{
	public sealed class InvertControlBlock : AbstractControlBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public InvertControlBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
