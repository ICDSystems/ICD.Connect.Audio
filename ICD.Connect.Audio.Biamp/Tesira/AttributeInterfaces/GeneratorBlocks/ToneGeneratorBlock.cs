namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.GeneratorBlocks
{
	public sealed class ToneGeneratorBlock : AbstractGeneratorBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public ToneGeneratorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
