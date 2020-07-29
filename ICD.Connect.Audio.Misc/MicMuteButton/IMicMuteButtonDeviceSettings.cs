using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Misc.MicMuteButton
{
	public interface IMicMuteButtonDeviceSettings: IDeviceSettings
	{
		[OriginatorIdSettingsProperty(typeof(IDigitalInputPort))]
		int? ButtonInputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IIoPort))]
		int? VoltageInputPort { get; set; }
	}
}