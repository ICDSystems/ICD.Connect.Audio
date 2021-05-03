using ICD.Common.Properties;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public sealed class CameraSwitcherNamedComponent : AbstractSwitcherNamedComponent
	{
		[UsedImplicitly]
		public CameraSwitcherNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml) : base(id, friendlyName, context, xml)
		{
		}

		[UsedImplicitly]
		public CameraSwitcherNamedComponent(int id, CoreElementsLoadContext context, string componentName) : base(id, context, componentName)
		{
		}
	}
}
