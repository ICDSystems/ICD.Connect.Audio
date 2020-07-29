namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.DelayBlocks
{
	public sealed class AudioDelayBlock : AbstractDelayBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public AudioDelayBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
