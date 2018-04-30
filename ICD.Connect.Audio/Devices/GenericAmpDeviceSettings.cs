using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Devices
{
	[KrangSettings("GenericAmpDevice", typeof(GenericAmpDevice))]
	public sealed class GenericAmpDeviceSettings : AbstractDeviceSettings
	{
	}
}
