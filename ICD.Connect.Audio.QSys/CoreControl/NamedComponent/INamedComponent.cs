using System;
using System.Collections.Generic;
using System.Text;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedComponent
{
    public interface INamedComponent : IDisposable
    {
		string ComponentName { get; }
    }
}
