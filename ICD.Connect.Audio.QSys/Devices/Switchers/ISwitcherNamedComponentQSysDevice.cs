using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;

namespace ICD.Connect.Audio.QSys.Devices.Switchers
{
	public interface
		ISwitcherNamedComponentQSysDevice<TSwitcherNamedComponent> : INamedComponentQSysDevice<TSwitcherNamedComponent>,
		                                                             ISwitcherNamedComponentQSysDevice
		where TSwitcherNamedComponent : class, ISwitcherNamedComponent
	{
	}

	public interface ISwitcherNamedComponentQSysDevice : INamedComponentQSysDevice
	{
	}
}
