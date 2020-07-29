namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.ControlBlocks
{
	public sealed class PresetControlBlock : AbstractControlBlock
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public PresetControlBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
		}
	}
}
