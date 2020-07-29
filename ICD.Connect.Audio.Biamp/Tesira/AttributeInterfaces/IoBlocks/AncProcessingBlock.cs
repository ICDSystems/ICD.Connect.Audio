namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks
{
	public sealed class AncProcessingBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public AncProcessingBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
