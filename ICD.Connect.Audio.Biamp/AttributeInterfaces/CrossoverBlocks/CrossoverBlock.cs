namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.CrossoverBlocks
{
	public sealed class CrossoverBlock : AbstractCrossoverBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public CrossoverBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
