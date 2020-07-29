namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MixerBlocks.StandardMixer
{
	public sealed class StandardMixerOutput : AbstractStandardMixerIo
	{
		private const string OUTPUT_LABEL_ATTRIBUTE = "outputLabel";
		private const string OUTPUT_LEVEL_ATTRIBUTE = "outputLevel";
		private const string OUTPUT_MIN_LEVEL_ATTRIBUTE = "outputMinLevel";
		private const string OUTPUT_MAX_LEVEL_ATTRIBUTE = "outputMaxLevel";
		private const string OUTPUT_MUTE_ATTRIBUTE = "outputMute";

		#region Properties

		protected override string LabelAttribute { get { return OUTPUT_LABEL_ATTRIBUTE; } }

		protected override string LevelAttribute { get { return OUTPUT_LEVEL_ATTRIBUTE; } }

		protected override string MinLevelAttribute { get { return OUTPUT_MIN_LEVEL_ATTRIBUTE; } }

		protected override string MaxLevelAttribute { get { return OUTPUT_MAX_LEVEL_ATTRIBUTE; } }

		protected override string MuteAttribute { get { return OUTPUT_MUTE_ATTRIBUTE; } }


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Output"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public StandardMixerOutput(StandardMixerBlock parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}
	}
}
