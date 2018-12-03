using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.Controls
{
	public interface IVolumeMuteBasicDeviceControl : IVolumeDeviceControl
	{
		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		[ApiMethod(VolumeMuteBasicDeviceControlApi.METHOD_VOLUME_MUTE_TOGGLE,
			VolumeMuteBasicDeviceControlApi.HELP_METHOD_VOLUME_MUTE_TOGGLE)]
		void VolumeMuteToggle();
	}
}
