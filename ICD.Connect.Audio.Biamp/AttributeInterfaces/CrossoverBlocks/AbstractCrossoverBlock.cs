namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.CrossoverBlocks
{
	public abstract class AbstractCrossoverBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractCrossoverBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
