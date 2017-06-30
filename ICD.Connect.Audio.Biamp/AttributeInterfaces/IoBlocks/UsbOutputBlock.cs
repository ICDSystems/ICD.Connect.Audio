namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public sealed class UsbOutputBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public UsbOutputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
