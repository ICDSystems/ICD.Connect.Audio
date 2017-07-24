using System;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GatingAutoMixer
{
	/// <summary>
	/// The API for mic logic kinda sucks. It's an enum, but it uses CHAN1, CHAN2, etc.
	/// This block supports 256 channels :/
	/// Instead I'm just going to use ints and translate them.
	/// </summary>
	public static class MicLogic
	{
		private const int MIC_LOGIC_LASTHOLD = -1;
		private const int MIC_LOGIC_NONE = 0;

		public const string MIC_LOGIC_NONE_SERIAL = "NONE";
		public const string MIC_LOGIC_LASTHOLD_SERIAL = "LASTHOLD";
		public const string MIC_LOGIC_CHAN_PREFIX = "CHAN";

		public static int SerialToMicLogic(string value)
		{
			value = value.Trim().ToUpper();

			switch (value)
			{
				case MIC_LOGIC_LASTHOLD_SERIAL:
					return MIC_LOGIC_LASTHOLD;
				case MIC_LOGIC_NONE_SERIAL:
					return MIC_LOGIC_NONE;
			}

			if (value.StartsWith(MIC_LOGIC_CHAN_PREFIX))
				return int.Parse(value.Substring(MIC_LOGIC_CHAN_PREFIX.Length));

			string message = string.Format("No {0} for serial {1}", typeof(MicLogic).Name, value);
			throw new ArgumentOutOfRangeException(message);
		}

		public static string MicLogicToSerial(int micLogic)
		{
			switch (micLogic)
			{
				case MIC_LOGIC_LASTHOLD:
					return MIC_LOGIC_LASTHOLD_SERIAL;
				case MIC_LOGIC_NONE:
					return MIC_LOGIC_NONE_SERIAL;
			}

			if (micLogic > 0)
				return string.Format("{0}{1}", MIC_LOGIC_CHAN_PREFIX, micLogic);

			string message = string.Format("No serial for {0} value {1}", typeof(MicLogic).Name, micLogic);
			throw new ArgumentOutOfRangeException(message);
		}
	}
}
