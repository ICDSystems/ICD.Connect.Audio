using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Audio.VolumePoints
{
	public interface IVolumePoint : IOriginator
	{
		/// <summary>
		/// Device id
		/// </summary>
		int DeviceId { get; set; }

		/// <summary>
		/// Control id.
		/// </summary>
		int ControlId { get; set; }
	}
}
