namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.Services
{
	public abstract class AbstractService : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractService(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
