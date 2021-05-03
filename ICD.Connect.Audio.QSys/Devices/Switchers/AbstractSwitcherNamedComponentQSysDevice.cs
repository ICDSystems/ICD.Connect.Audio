using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;

namespace ICD.Connect.Audio.QSys.Devices.Switchers
{
	public abstract class AbstractSwitcherNamedComponentQSysDevice<TSettings, TComponent>: AbstractNamedComponentQSysDevice<TSettings, TComponent>, ISwitcherNamedComponentQSysDevice<TComponent> where TComponent: class, ISwitcherNamedComponent where TSettings: INamedComponentQSysDeviceSettings, new()
	{
	}
}
