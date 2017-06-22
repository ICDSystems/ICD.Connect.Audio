namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.EqualizerBlocks
{
	public sealed class GraphicEqualizerBlock : AbstractEqualizerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public GraphicEqualizerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
