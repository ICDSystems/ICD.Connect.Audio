namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.DynamicBlocks
{
	public sealed class LevelerBlock : AbstractDynamicBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public LevelerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
