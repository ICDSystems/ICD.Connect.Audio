using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Misc.BiColorMicButton
{
	public interface IBiColorMicButtonDeviceSettings: IDeviceSettings
	{

		[OriginatorIdSettingsProperty(typeof(IDigitalInputPort))]
		int? ButtonInputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IIoPort))]
		int? VoltageInputPort { get; set; }
	}
}