namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.FilterBlocks
{
	public sealed class AllPassFilterBlock : AbstractFilterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public AllPassFilterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
