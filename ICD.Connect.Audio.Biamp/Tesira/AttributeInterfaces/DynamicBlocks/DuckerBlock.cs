namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DynamicBlocks
{
	public sealed class DuckerBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public DuckerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
