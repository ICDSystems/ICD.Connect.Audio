namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.EqualizerBlocks
{
	public sealed class GraphicEqualizerBlock : AbstractEqualizerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public GraphicEqualizerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
