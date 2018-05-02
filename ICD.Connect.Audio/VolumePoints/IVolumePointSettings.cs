using ICD.Connect.Settings;

namespace ICD.Connect.Audio.VolumePoints
{
	public interface IVolumePointSettings : ISettings
	{
		int DeviceId { get; set; }

		int ControlId { get; set; }
	}
}
