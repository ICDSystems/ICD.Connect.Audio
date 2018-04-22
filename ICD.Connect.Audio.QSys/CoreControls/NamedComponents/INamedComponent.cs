using System;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
    public interface INamedComponent : IDeviceControl
    {
		string ComponentName { get; }
    }
}
