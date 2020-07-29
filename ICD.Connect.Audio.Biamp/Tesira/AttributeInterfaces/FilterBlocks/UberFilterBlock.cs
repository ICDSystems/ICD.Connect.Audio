namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.FilterBlocks
{
	public sealed class UberFilterBlock : AbstractFilterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public UberFilterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
