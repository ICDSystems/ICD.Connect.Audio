using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public interface ITesiraChildDeviceSettings : IDeviceSettings
	{
		[OriginatorIdSettingsProperty(typeof(BiampTesiraDevice))]
		int? BiampId { get; set; }
	}
}