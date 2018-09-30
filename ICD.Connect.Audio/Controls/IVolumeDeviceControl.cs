using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// This interface just acts as a roll-up for <see cref="IVolumeRampDeviceControl"/> and <see cref="IVolumeMuteBasicDeviceControl"/>
	/// </summary>
	public interface IVolumeDeviceControl : IDeviceControl
	{
	}
}
