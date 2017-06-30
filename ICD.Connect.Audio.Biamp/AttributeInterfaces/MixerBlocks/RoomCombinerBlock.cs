namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
	public sealed class RoomCombinerBlock : AbstractMixerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public RoomCombinerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
