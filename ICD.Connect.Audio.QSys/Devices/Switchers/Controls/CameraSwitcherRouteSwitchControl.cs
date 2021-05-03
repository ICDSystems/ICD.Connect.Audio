using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.Switchers.CameraSwitcher;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.Controls
{
	class CameraSwitcherRouteSwitchControl : AbstractSwitcherRouteSwitchControl<CameraSwitcherQSysDevice, CameraSwitcherNamedComponent>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CameraSwitcherRouteSwitchControl(CameraSwitcherQSysDevice parent, int id) : base(parent, id, eConnectionType.Video)
		{
		}
	}
}
