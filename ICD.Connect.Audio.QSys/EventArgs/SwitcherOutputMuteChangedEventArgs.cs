using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.QSys.EventArgs
{
	public sealed class SwitcherOutputMuteChangedEventArgs : GenericEventArgs<SwitcherOutputMuteChangedData>
	{
		public int Output { get { return Data.Output; } }

		public bool MuteState { get { return Data.MuteState; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SwitcherOutputMuteChangedEventArgs(SwitcherOutputMuteChangedData data) : base(data)
		{
		}

		public SwitcherOutputMuteChangedEventArgs(int output, bool muteState) :
			this(new SwitcherOutputMuteChangedData(output, muteState))
		{
		}
	}

	public sealed class SwitcherOutputMuteChangedData
	{
		public int Output { get; private set; }
		public bool MuteState { get; private set; }

		public SwitcherOutputMuteChangedData(int output, bool muteState)
		{
			Output = output;
			MuteState = muteState;
		}
	}
}
