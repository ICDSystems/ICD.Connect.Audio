using ICD.Connect.Devices.Points;

namespace ICD.Connect.Audio.VolumePoints
{
	/// <summary>
	/// Used by the metlife room to better manage volume controls.
	/// </summary>
	public abstract class AbstractVolumePoint<TSettings> : AbstractPoint<TSettings>, IVolumePoint
		where TSettings : IVolumePointSettings, new()
	{
	}
}
