using System;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
    public interface INamedComponent : IDisposable
    {
		string ComponentName { get; }
    }
}
