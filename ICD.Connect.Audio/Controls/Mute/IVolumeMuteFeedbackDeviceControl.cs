using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Mute;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Controls.Mute
{
	/// <summary>
	/// IVolumeMuteFeedbackDeviceControl is for devices that offer mute state control and feedback
	/// </summary>
	public interface IVolumeMuteFeedbackDeviceControl : IVolumeMuteDeviceControl
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		[ApiEvent(VolumeMuteFeedbackDeviceControlApi.EVENT_MUTE_STATE_CHANGED,
			VolumeMuteFeedbackDeviceControlApi.HELP_EVENT_MUTE_STATE_CHANGED)]
		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_MUTE_CHANGED)]
		event EventHandler<MuteDeviceMuteStateChangedApiEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[ApiProperty(VolumeMuteFeedbackDeviceControlApi.PROPERTY_VOLUME_IS_MUTED,
			VolumeMuteFeedbackDeviceControlApi.HELP_PROPERTY_VOLUME_IS_MUTED)]
		[DynamicPropertyTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_MUTE, VolumeTelemetryNames.VOLUME_CONTROL_MUTE_CHANGED)]
		bool VolumeIsMuted { get; }

		#endregion
	}
}
