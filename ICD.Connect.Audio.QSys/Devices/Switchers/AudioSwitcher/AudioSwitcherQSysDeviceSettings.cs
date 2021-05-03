using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.AudioSwitcher
{
	[KrangSettings("QSysCoreAudioSwitcher", typeof(AudioSwitcherQSysDevice))]
	public sealed class AudioSwitcherQSysDeviceSettings : AbstractNamedComponentQSysDeviceSettings
	{
	}
}
