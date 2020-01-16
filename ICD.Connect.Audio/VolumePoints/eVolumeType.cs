namespace ICD.Connect.Audio.VolumePoints
{
	public enum eVolumeType
	{
		/// <summary>
		/// Room audio.
		/// </summary>
		Room,

		/// <summary>
		/// Source audio.
		/// </summary>
		Program,

		/// <summary>
		/// Video Conference audio.
		/// </summary>
		Vtc,

		/// <summary>
		/// Audio Conference audio.
		/// </summary>
		Atc,

		/// <summary>
		/// Speech reinforcement.
		/// </summary>
		Sr,

		/// <summary>
		/// Background music.
		/// </summary>
		Bgm,

		/// <summary>
		/// Microphone audio.
		/// </summary>
		Mic,

		/// <summary>
		/// Audio with no attached logic.
		/// </summary>
		Generic
	}
}
