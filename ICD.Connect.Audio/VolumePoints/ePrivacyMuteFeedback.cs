using System;

namespace ICD.Connect.Audio.VolumePoints
{
	[Flags]
	public enum ePrivacyMuteFeedback
	{
		None = 0,

		/// <summary>
		/// The privacy mute control provides feedback for the current privacy mute state.
		/// </summary>
		Get = 1,

		/// <summary>
		/// The privacy mute control privacy mute state can be set by the control system.
		/// </summary>
		Set = 2,

        /// <summary>
        /// The control can provide get for current muted state, but will not unmute
        /// </summary>
        GetMutedOnly = 4,

		GetSet = Get | Set
	}
}
