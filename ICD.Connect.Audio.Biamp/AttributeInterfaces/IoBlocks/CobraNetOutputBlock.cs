namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public sealed class CobraNetOutputBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public CobraNetOutputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
