namespace ICD.Connect.Audio.VolumePoints
{
	public enum eMuteType
	{
		/// <summary>
		/// Not used for any mute action.
		/// </summary>
		None,

		/// <summary>
		/// Mutes the room audio output.
		/// </summary>
		RoomAudio,

		/// <summary>
		/// Mutes audio input at the DSP level.
		/// </summary>
		DspPrivacyMute,

		/// <summary>
		/// Mutes audio input at the microphone level.
		/// </summary>
		MicPrivacyMute
	}
}
