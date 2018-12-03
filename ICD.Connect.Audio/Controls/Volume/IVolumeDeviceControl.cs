using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls.Volume
{
	/// <summary>
	/// This interface just acts as a roll-up for <see cref="IVolumeRampDeviceControl"/> and <see cref="IVolumeMuteBasicDeviceControl"/>
	/// </summary>
	public interface IVolumeDeviceControl : IDeviceControl
	{
	}
}
