namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.VoIp
{
	public sealed class VoIpCallStateCommands : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public VoIpCallStateCommands(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
