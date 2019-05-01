using System;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls
{
	public interface IQSysCoreControl: IConsoleNodeBase, IDisposable
	{
		QSysCoreDevice QSysCore { get; }
		string Name { get; }
		int Id { get; }
	}
}