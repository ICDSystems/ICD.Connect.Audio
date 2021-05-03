using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.QSys.EventArgs
{
	public sealed class SwitcherOutputSelectChangedEventArgs : GenericEventArgs<SwitcherOutputSelectChangedData>
	{
		public int Output { get { return Data.Output; } }

		public int SelectedInput { get { return Data.SelectedInput; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SwitcherOutputSelectChangedEventArgs(SwitcherOutputSelectChangedData data) : base(data)
		{
		}

		public SwitcherOutputSelectChangedEventArgs(int output, int selectedInput) :
			this(new SwitcherOutputSelectChangedData(output, selectedInput))
		{
		}
	}

	public sealed class SwitcherOutputSelectChangedData
	{
		public int Output { get; private set; }
		public int SelectedInput { get; private set; }

		public SwitcherOutputSelectChangedData(int output, int selectedInput)
		{
			Output = output;
			SelectedInput = selectedInput;
		}
	}
}
