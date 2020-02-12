using System;

namespace ICD.Connect.Audio.VolumePoints
{
	[Flags]
	public enum eVolumePointContext
	{
		None = 0,

		/// <summary>
		/// Room audio.
		/// </summary>
		Room = 1,

		/// <summary>
		/// Source audio.
		/// </summary>
		Program = 2,

		/// <summary>
		/// Audio Conference audio.
		/// </summary>
		Atc = 4,

		/// <summary>
		/// Video Conference audio.
		/// </summary>
		Vtc = 8,

		/// <summary>
		/// Speech reinforcement.
		/// </summary>
		Sr = 16,

		/// <summary>
		/// Background music.
		/// </summary>
		Bgm = 32,

		/// <summary>
		/// Microphone audio.
		/// </summary>
		Mic = 64,

		/// <summary>
		/// Audio with no attached logic.
		/// </summary>
		Generic = 128
	}
}
