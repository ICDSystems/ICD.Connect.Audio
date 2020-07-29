namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks
{
	public sealed class CobraNetOutputBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public CobraNetOutputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
