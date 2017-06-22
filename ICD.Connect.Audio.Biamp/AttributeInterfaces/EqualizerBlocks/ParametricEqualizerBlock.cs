namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.EqualizerBlocks
{
	public sealed class ParametricEqualizerBlock : AbstractEqualizerBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		public ParametricEqualizerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
