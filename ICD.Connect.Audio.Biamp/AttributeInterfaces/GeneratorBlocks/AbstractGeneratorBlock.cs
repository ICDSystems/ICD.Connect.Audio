namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.GeneratorBlocks
{
	public abstract class AbstractGeneratorBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		protected AbstractGeneratorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
