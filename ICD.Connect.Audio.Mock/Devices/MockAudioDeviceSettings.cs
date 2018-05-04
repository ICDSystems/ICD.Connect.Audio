using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Mock.Devices
{
	[KrangSettings("MockAudioDevice", typeof(MockAudioDevice))]
	public sealed class MockAudioDeviceSettings : AbstractDeviceSettings
	{
	}
}
