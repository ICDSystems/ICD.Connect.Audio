namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public sealed class DtmfDecodeBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public DtmfDecodeBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
