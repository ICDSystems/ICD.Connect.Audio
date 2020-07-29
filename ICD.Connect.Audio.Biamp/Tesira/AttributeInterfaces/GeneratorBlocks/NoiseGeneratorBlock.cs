namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.GeneratorBlocks
{
	public sealed class NoiseGeneratorBlock : AbstractGeneratorBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public NoiseGeneratorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
