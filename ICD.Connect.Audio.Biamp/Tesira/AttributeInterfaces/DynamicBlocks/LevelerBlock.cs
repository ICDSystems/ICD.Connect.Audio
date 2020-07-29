namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DynamicBlocks
{
	public sealed class LevelerBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public LevelerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
