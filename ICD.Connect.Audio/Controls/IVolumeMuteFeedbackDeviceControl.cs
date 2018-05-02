using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

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
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[PublicAPI]
		bool VolumeIsMuted { get; }

		#endregion
	}
}
