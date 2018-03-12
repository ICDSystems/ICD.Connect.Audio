using System;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedComponent
{
    public interface INamedComponent : IDisposable
    {
		string ComponentName { get; }
    }
}
