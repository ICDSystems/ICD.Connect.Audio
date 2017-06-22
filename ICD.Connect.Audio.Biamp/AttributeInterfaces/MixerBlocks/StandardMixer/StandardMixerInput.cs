namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.StandardMixer
{
	public sealed class StandardMixerInput : AbstractStandardMixerIo
	{
		private const string INPUT_LABEL_ATTRIBUTE = "inputLabel";
		private const string INPUT_LEVEL_ATTRIBUTE = "inputLevel";
		private const string INPUT_MIN_LEVEL_ATTRIBUTE = "inputMinLevel";
		private const string INPUT_MAX_LEVEL_ATTRIBUTE = "inputMaxLevel";
		private const string INPUT_MUTE_ATTRIBUTE = "inputMute";

		#region Properties

		protected override string LabelAttribute { get { return INPUT_LABEL_ATTRIBUTE; } }

		protected override string LevelAttribute { get { return INPUT_LEVEL_ATTRIBUTE; } }

		protected override string MinLevelAttribute { get { return INPUT_MIN_LEVEL_ATTRIBUTE; } }

		protected override string MaxLevelAttribute { get { return INPUT_MAX_LEVEL_ATTRIBUTE; } }

		protected override string MuteAttribute { get { return INPUT_MUTE_ATTRIBUTE; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public StandardMixerInput(StandardMixerBlock parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}
	}
}
