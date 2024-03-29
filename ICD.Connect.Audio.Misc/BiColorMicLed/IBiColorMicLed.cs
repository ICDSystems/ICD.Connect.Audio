﻿using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Misc.BiColorMicLed
{
	public interface IBiColorMicLed : IDevice
	{
		/// <summary>
		/// Raised when the red LED enabled state changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnPowerEnabledChanged;

		/// <summary>
		/// Raised when the red LED enabled state changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnRedLedEnabledChanged;

		/// <summary>
		/// Raised when the green LED enabled state changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnGreenLedEnabledChanged;

		/// <summary>
		/// Gets the enabled state of the power output.
		/// </summary>
		[PublicAPI]
		bool PowerEnabled { get; }

		/// <summary>
		/// Gets the enabled state of the Red LED.
		/// </summary>
		[PublicAPI]
		bool RedLedEnabled { get; }

		/// <summary>
		/// Gets the enabled state of the green LED.
		/// </summary>
		[PublicAPI]
		bool GreenLedEnabled { get; }

		/// <summary>
		/// Turns on/off the controller power.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetPowerEnabled(bool enabled);

		/// <summary>
		/// Turns on/off the ring of red LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetRedLedEnabled(bool enabled);

		/// <summary>
		/// Turns on/off the ring of green LEDs.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetGreenLedEnabled(bool enabled);
	}
}
