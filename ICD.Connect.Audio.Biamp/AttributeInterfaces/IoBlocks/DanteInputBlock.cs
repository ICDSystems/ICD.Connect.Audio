namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public sealed class DanteInputBlock : AbstractIoBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public DanteInputBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
