namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.FilterBlocks
{
	public sealed class PassFilterBlock : AbstractFilterBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public PassFilterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
