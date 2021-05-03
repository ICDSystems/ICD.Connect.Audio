﻿using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.CameraSwitcher
{
	[KrangSettings("QSysCoreCameraSwitcher", typeof(CameraSwitcherQSysDevice))]
	class CameraSwitcherQSysDeviceSettings : AbstractNamedComponentQSysDeviceSettings
	{
	}
}
