using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Volume;
using ICD.Connect.Audio.Telemetry;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Controls.Volume
{
	[Flags]
	public enum eVolumeFeatures
	{
		/// <summary>
		/// The control supports no volume/mute features.
		/// </summary>
		None = 0,

		/// <summary>
		/// The control supports mute toggle.
		/// </summary>
		Mute = 1,

		/// <summary>
		/// The control will report mute state changes.
		/// </summary>
		MuteFeedback = 2,

		/// <summary>
		/// The control supports setting the mute state directly.
		/// </summary>
		MuteAssignment = 4,

		/// <summary>
		/// The control supports volume increment and decrement.
		/// </summary>
		Volume = 8,

		/// <summary>
		/// The control will report volume level changes.
		/// </summary>
		VolumeFeedback = 16,

		/// <summary>
		/// The control supports setting the volume level directly, as well as incremental ramping.
		/// </summary>
		VolumeAssignment = 32,

		/// <summary>
		/// The control supports holding a volume direction for continuous ramping until stopped.
		/// I.e. an IR display may hold the IR command for volume until releasing.
		/// </summary>
		VolumeRamp = 64
	}

	public interface IVolumeDeviceControl : IDeviceControl
	{
		#region Events

		/// <summary>
		/// Raised when the mute state changes.
		/// Will not raise if mute feedback is not supported.
		/// </summary>
		[ApiEvent(VolumeDeviceControlApi.EVENT_IS_MUTED_CHANGED, VolumeDeviceControlApi.HELP_EVENT_IS_MUTED_CHANGED)]
		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_IS_MUTED_CHANGED)]
		event EventHandler<VolumeControlIsMutedChangedApiEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the volume changes.
		/// Will not raise if volume feedback is not supported.
		/// </summary>
		[ApiEvent(VolumeDeviceControlApi.EVENT_VOLUME_CHANGED, VolumeDeviceControlApi.HELP_EVENT_VOLUME_CHANGED)]
		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_VOLUME_CHANGED)]
		event EventHandler<VolumeControlVolumeChangedApiEventArgs> OnVolumeChanged;

		#endregion

		#region Support

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_SUPPORTED_VOLUME_FEATURES, VolumeDeviceControlApi.HELP_PROPERTY_SUPPORTS_VOLUME_FEATURES)]
		eVolumeFeatures SupportedVolumeFeatures { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// Will return false if mute feedback is not supported.
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_IS_MUTED, VolumeDeviceControlApi.HELP_PROPERTY_IS_MUTED)]
		[DynamicPropertyTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_IS_MUTED, VolumeTelemetryNames.VOLUME_CONTROL_IS_MUTED_CHANGED)]
		bool IsMuted { get; }

		/// <summary>
		/// Gets the current volume in the range VolumeLevelMin to VolumeLevelMax.
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL, VolumeDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL)]
		float VolumeLevel { get; }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MIN, VolumeDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MIN)]
		float VolumeLevelMin { get; }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MAX, VolumeDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MAX)]
		float VolumeLevelMax { get; }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		[ApiProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_STRING, VolumeDeviceControlApi.HELP_PROPERTY_VOLUME_STRING)]
		string VolumeString { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		[ApiMethod(VolumeDeviceControlApi.METHOD_SET_IS_MUTED, VolumeDeviceControlApi.HELP_METHOD_SET_IS_MUTED)]
		[MethodTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_MUTE_COMMAND)]
		void SetIsMuted(bool mute);

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		[ApiMethod(VolumeDeviceControlApi.METHOD_TOGGLE_IS_MUTED, VolumeDeviceControlApi.HELP_METHOD_TOGGLE_IS_MUTED)]
		void ToggleIsMuted();

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		[ApiMethod(VolumeDeviceControlApi.METHOD_SET_VOLUME_LEVEL, VolumeDeviceControlApi.HELP_METHOD_SET_VOLUME_LEVEL)]
		void SetVolumeLevel(float level);

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeDeviceControlApi.METHOD_VOLUME_INCREMENT, VolumeDeviceControlApi.HELP_METHOD_VOLUME_INCREMENT)]
		void VolumeIncrement();

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeDeviceControlApi.METHOD_VOLUME_DECREMENT, VolumeDeviceControlApi.HELP_METHOD_VOLUME_DECREMENT)]
		void VolumeDecrement();

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		[ApiMethod(VolumeDeviceControlApi.METHOD_VOLUME_RAMP, VolumeDeviceControlApi.HELP_METHOD_VOLUME_RAMP)]
		void VolumeRamp(bool increment, long timeout);

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		[ApiMethod(VolumeDeviceControlApi.METHOD_VOLUME_RAMP_STOP, VolumeDeviceControlApi.HELP_METHOD_VOLUME_RAMP_STOP)]
		void VolumeRampStop();

		#endregion
	}

	public static class VolumeDeviceControlExtensions
	{
		#region Level

		/// <summary>
		/// Increments the volume.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="incrementValue"></param>
		public static void VolumeLevelIncrement([NotNull] this IVolumeDeviceControl extends, float incrementValue)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			float newLevel = extends.VolumeLevel + incrementValue;
			extends.SetVolumeLevel(newLevel);
		}

		/// <summary>
		/// Decrements the volume.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="decrementValue"></param>
		public static void VolumeLevelDecrement([NotNull] this IVolumeDeviceControl extends, float decrementValue)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			float newLevel = extends.VolumeLevel - decrementValue;
			extends.SetVolumeLevel(newLevel);
		}

		/// <summary>
		/// Clamps the given volume level to the min/max of the control.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public static float ClampToVolumeLevel([NotNull] this IVolumeDeviceControl extends, float level)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return MathUtils.Clamp(level, extends.VolumeLevelMin, extends.VolumeLevelMax);
		}

		#endregion

		#region Percent

		/// <summary>
		/// Convert from a volume level to a percentage.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="level">Volume Level</param>
		/// <returns>Volume percent, between 0 and 1</returns>
		public static float ConvertLevelToPercent([NotNull] this IVolumeDeviceControl extends, float level)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return MathUtils.ToPercent(extends.VolumeLevelMin, extends.VolumeLevelMax, level);
		}

		/// <summary>
		/// Convert from a percentage to a volume level.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="percent">Volume percent, between 0 and 1</param>
		/// <returns>Volume Level</returns>
		public static float ConvertPercentToLevel([NotNull] this IVolumeDeviceControl extends, float percent)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return MathUtils.FromPercent(extends.VolumeLevelMin, extends.VolumeLevelMax, percent);
		}

		/// <summary>
		/// Gets the volume percent, 0 - 1
		/// </summary>
		/// <param name="extends"></param>
		public static float GetVolumePercent([NotNull] this IVolumeDeviceControl extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.ConvertLevelToPercent(extends.VolumeLevel);
		}

		/// <summary>
		/// Sets the volume percent, 0 - 1
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="percent"></param>
		public static void SetVolumePercent([NotNull] this IVolumeDeviceControl extends, float percent)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			float level = extends.ConvertPercentToLevel(percent);
			extends.SetVolumeLevel(level);
		}

		#endregion
	}
}
