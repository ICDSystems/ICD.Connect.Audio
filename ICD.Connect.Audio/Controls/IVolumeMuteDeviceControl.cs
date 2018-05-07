using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// IVolumeMuteDeviceControl is for devices that offer both toggle and direct set for mute state
	/// </summary>
	public interface IVolumeMuteDeviceControl : IVolumeMuteBasicDeviceControl
	{
		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		[ApiMethod(VolumeMuteDeviceControlApi.METHOD_SET_VOLUME_MUTE, VolumeMuteDeviceControlApi.HELP_METHOD_SET_VOLUME_MUTE)]
		void SetVolumeMute(bool mute);

		#endregion
	}
}
