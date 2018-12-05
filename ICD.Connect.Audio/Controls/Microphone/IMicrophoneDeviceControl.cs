using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls.Microphone;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls.Microphone
{
	public interface IMicrophoneDeviceControl : IDeviceControl
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		[ApiEvent(MicrophoneDeviceControlApi.EVENT_MUTE_STATE_CHANGED,
			MicrophoneDeviceControlApi.HELP_EVENT_MUTE_STATE_CHANGED)]
		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the phantom power state changes.
		/// </summary>
		[ApiEvent(MicrophoneDeviceControlApi.EVENT_PHANTOM_POWER_STATE_CHANGED,
			MicrophoneDeviceControlApi.HELP_EVENT_PHANTOM_POWER_STATE_CHANGED)]
		event EventHandler<BoolEventArgs> OnPhantomPowerStateChanged;

		/// <summary>
		/// Raised when the gain level changes.
		/// </summary>
		[ApiEvent(MicrophoneDeviceControlApi.EVENT_GAIN_LEVEL_CHANGED,
			MicrophoneDeviceControlApi.HELP_EVENT_GAIN_LEVEL_CHANGED)]
		event EventHandler<FloatEventArgs> OnGainLevelChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[ApiProperty(MicrophoneDeviceControlApi.PROPERTY_IS_MUTED, MicrophoneDeviceControlApi.HELP_PROPERTY_IS_MUTED)]
		bool IsMuted { get; }

		/// <summary>
		/// Gets the phantom power state.
		/// </summary>
		[ApiProperty(MicrophoneDeviceControlApi.PROPERTY_PHANTOM_POWER,
			MicrophoneDeviceControlApi.HELP_PROPERTY_PHANTOM_POWER)]
		bool PhantomPower { get; }

		/// <summary>
		/// Gets the gain level.
		/// </summary>
		[ApiProperty(MicrophoneDeviceControlApi.PROPERTY_GAIN_LEVEL, MicrophoneDeviceControlApi.HELP_PROPERTY_GAIN_LEVEL)]
		float GainLevel { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		[ApiMethod(MicrophoneDeviceControlApi.METHOD_SET_GAIN_LEVEL, MicrophoneDeviceControlApi.HELP_METHOD_SET_GAIN_LEVEL)]
		void SetGainLevel(float volume);

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		[ApiMethod(MicrophoneDeviceControlApi.METHOD_SET_MUTED, MicrophoneDeviceControlApi.HELP_METHOD_SET_MUTED)]
		void SetMuted(bool mute);

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		[ApiMethod(MicrophoneDeviceControlApi.METHOD_SET_PHANTOM_POWER,
			MicrophoneDeviceControlApi.HELP_METHOD_SET_PHANTOM_POWER)]
		void SetPhantomPower(bool power);

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		[ApiMethod(MicrophoneDeviceControlApi.METHOD_MUTE_TOGGLE, MicrophoneDeviceControlApi.HELP_METHOD_MUTE_TOGGLE)]
		void MuteToggle();

		/// <summary>
		/// Toggles the current phantom power state.
		/// </summary>
		[ApiMethod(MicrophoneDeviceControlApi.METHOD_PHANTOM_POWER_TOGGLE,
			MicrophoneDeviceControlApi.HELP_METHOD_PHANTOM_POWER_TOGGLE)]
		void PhantomPowerToggle();

		#endregion
	}
}
