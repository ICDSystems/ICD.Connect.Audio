namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MeterBlocks
{
	public abstract class AbstractMeterBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		protected AbstractMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
