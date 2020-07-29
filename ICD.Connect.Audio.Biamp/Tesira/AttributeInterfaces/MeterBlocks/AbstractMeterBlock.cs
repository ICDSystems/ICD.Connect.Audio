namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MeterBlocks
{
	public abstract class AbstractMeterBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
