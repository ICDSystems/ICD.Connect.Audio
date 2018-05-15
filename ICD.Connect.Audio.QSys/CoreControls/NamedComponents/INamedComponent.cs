using System;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
    public interface INamedComponent : IDeviceControl
    {
		string ComponentName { get; }

		// Also needs to implement constructors like:
		// INamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml);
		// and
		// INamedComponent(int id, CoreElementsLoadContext context, string componentName);
    }
}
