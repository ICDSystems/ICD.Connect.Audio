using System;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.CoreControls
{
	public interface IQSysCoreControl: IConsoleNodeBase, IDisposable
	{
		string Name { get; }
		int Id { get; }
	}
}