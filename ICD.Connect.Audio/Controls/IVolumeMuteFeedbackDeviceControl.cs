using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.Controls
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
		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[ApiProperty(VolumeMuteFeedbackDeviceControlApi.PROPERTY_VOLUME_IS_MUTED,
			VolumeMuteFeedbackDeviceControlApi.HELP_PROPERTY_VOLUME_IS_MUTED)]
		bool VolumeIsMuted { get; }

		#endregion
	}
}
