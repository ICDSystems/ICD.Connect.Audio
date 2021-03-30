using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Devices.Microphones
{
	public interface IMicrophoneDevice : IDevice
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the phantom power state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPhantomPowerStateChanged;

		/// <summary>
		/// Raised when the gain level changes.
		/// </summary>
		event EventHandler<FloatEventArgs> OnGainLevelChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Gets the phantom power state.
		/// </summary>
		bool PhantomPower { get; }

		/// <summary>
		/// Gets the gain level.
		/// </summary>
		float GainLevel { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		void SetGainLevel(float volume);

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		void SetMuted(bool mute);

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		void SetPhantomPower(bool power);

		#endregion
	}
}
