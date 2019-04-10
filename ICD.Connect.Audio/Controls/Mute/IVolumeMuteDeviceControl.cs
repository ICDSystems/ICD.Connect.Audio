using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls.Mute;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Controls.Mute
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
		[MethodTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_MUTE_COMMAND)]
		void SetVolumeMute(bool mute);

		#endregion
	}
}
