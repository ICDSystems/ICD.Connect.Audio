namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.DelayBlocks
{
	public sealed class AudioDelayBlock : AbstractDelayBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public AudioDelayBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
