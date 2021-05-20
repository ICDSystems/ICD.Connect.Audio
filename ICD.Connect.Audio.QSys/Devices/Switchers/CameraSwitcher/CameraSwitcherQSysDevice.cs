using System;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.Switchers.Controls;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.CameraSwitcher
{
	public sealed class CameraSwitcherQSysDevice :
		AbstractSwitcherNamedComponentQSysDevice<CameraSwitcherQSysDeviceSettings, CameraSwitcherNamedComponent>
	{
		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(CameraSwitcherQSysDeviceSettings settings, IDeviceFactory factory,
		                                    Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new CameraSwitcherRouteSwitchControl(this, 0));
		}
	}
}
