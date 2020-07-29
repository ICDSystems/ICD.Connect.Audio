namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks
{
	public sealed class AudioOutputBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public AudioOutputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
