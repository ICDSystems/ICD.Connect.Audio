using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Devices.Microphones;

namespace ICD.Connect.Audio.Shure.Devices
{
	public interface IShureMicDevice : IMicrophoneDevice
	{
		/// <summary>
		/// Raised when the mute button is pressed/released.
		/// </summary>
		event EventHandler<BoolEventArgs> OnMuteButtonStatusChanged;

		/// <summary>
		/// Raised when the audio gain changes.
		/// </summary>
		event EventHandler<IntEventArgs> OnAudioGainChanged;

		/// <summary>
		/// Raised when the muted state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Gets the analog gain level.
		/// </summary>
		int AudioGain { get; }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="brightness"></param>
		void SetLedStatus(eLedColor color, eLedBrightness brightness);

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		void SetAudioGain(float volume);

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		void SetIsMuted(bool mute);
	}
}