using ICD.Connect.Devices;

namespace ICD.Connect.Audio.QSys.Devices
{
	public interface INamedComponentQSysDeviceSettings : IDeviceSettings
	{
		int DspId { get; set; }
		string ComponentName { get; set; }
	}
}