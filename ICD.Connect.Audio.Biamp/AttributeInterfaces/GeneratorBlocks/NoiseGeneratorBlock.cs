namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.GeneratorBlocks
{
	public sealed class NoiseGeneratorBlock : AbstractGeneratorBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public NoiseGeneratorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
