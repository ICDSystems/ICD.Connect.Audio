using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Devices.Microphones
{
	public abstract class AbstractMicrophoneDevice<TSettings> : AbstractDevice<TSettings>, IMicrophoneDevice
		where TSettings : IMicrophoneDeviceSettings, new()
	{
	}
}
