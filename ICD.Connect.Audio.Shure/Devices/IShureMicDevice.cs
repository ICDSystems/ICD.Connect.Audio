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
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="brightness"></param>
		void SetLedStatus(eLedColor color, eLedBrightness brightness);
	}
}