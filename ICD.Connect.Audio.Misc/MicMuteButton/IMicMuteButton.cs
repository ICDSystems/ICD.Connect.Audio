using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Misc.MicMuteButton
{
	public interface IMicMuteButton : IDevice
	{
		/// <summary>
		/// Raised when the microphone button is pressed.
		/// </summary>
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnButtonPressedChanged;

		/// <summary>
		/// Raised when the microphone voltage changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<UShortEventArgs> OnVoltageChanged;

		/// <summary>
		/// Gets the current presses state of the button.
		/// </summary>
		[PublicAPI]
		bool ButtonPressed { get; }

		/// <summary>
		/// Gets the voltage reported by the microphone hardware.
		/// </summary>
		[PublicAPI]
		ushort Voltage { get; }
	}
}