namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.RouterBlocks
{
	public abstract class AbstractRouterBlock : AbstractAttributeInterface
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		protected AbstractRouterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
