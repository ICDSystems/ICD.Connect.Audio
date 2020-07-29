namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks
{
	public sealed class TcCallStateCommands : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public TcCallStateCommands(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
