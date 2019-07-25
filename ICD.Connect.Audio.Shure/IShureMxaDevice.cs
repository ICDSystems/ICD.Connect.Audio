using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Shure
{
	public interface IShureMxaDevice : IShureMicDevice
	{

		/// <summary>
		/// Gets the mute button state.
		/// </summary>
		bool MuteButtonStatus { get; }

		/// <summary>
		/// Sets the brightness of the hardware LED.
		/// </summary>
		/// <param name="brightness"></param>
		void SetLedBrightness(eLedBrightness brightness);

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is muted.
		/// </summary>
		/// <param name="color"></param>
		void SetLedMuteColor(eLedColor color);

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is unmuted.
		/// </summary>
		/// <param name="color"></param>
		void SetLedUnmuteColor(eLedColor color);

		/// <summary>
		/// Sets the color of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		void SetLedColor(eLedColor color);

		/// <summary>
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="milliseconds"></param>
		void TurnMeteringOn(uint milliseconds);
	}
}