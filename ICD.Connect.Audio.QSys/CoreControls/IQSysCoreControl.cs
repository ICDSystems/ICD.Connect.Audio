using System;

namespace ICD.Connect.Audio.QSys.CoreControls
{
	public interface IQSysCoreControl: IDisposable
	{
		string Name { get; }
		int Id { get; }
	}
}