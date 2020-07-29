namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.FilterBlocks
{
	public sealed class ShelfFilterBlock : AbstractFilterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public ShelfFilterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
