using System;
using ICD.Connect.Audio.QSys.EventArgs;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public interface ISwitcherNamedComponent : INamedComponent
	{
		event EventHandler<SwitcherOutputSelectChangedEventArgs> OnOutputSelectChanged;
		int Inputs { get; }
		int Outputs { get; }
		int? TryGetOutputSelectState(int output);
		bool TrySetOutput(int output, int? input);
	}
}