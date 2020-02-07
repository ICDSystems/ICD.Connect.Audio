﻿using System;
using ICD.Common.Utils;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls.Volume;
using ICD.Connect.Audio.Utils;

namespace ICD.Connect.Audio.Controls.Volume
{
	/// <summary>
	/// IVolumeRawLevelDeviceControl is for devices that offer raw volume feedback and not position
	/// Volume Min/Max Range can be used to caculate position
	/// This interface should not be used to refer to volume controls, use IVolumeLevelDeviceControl instead
	/// </summary>
	public interface IVolumeLevelDeviceControl : IVolumePositionDeviceControl
	{
		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_RAW)]
		float VolumeLevel { get; }

		/// <summary>
		/// VolumeLevelMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		[ApiProperty(VolumeRawLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL_MAX_RANGE,
			VolumeRawLevelDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MAX_RANGE)]
		float VolumeLevelMaxRange { get; }

		/// <summary>
		/// VolumeLevelMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		[ApiProperty(VolumeRawLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL_MIN_RANGE,
			VolumeRawLevelDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MIN_RANGE)]
		float VolumeLevelMinRange { get; }

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_LEVEL, VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_LEVEL)]
		void SetVolumeLevel(float volume);

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_INCREMENT_STEP,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_INCREMENT)]
		void VolumeLevelIncrement(float incrementValue);

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_DECREMENT_STEP,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_DECREMENT)]
		void VolumeLevelDecrement(float decrementValue);
	}

	/// <summary>
	/// Extension methods for IVolumeRawLevelDeviceControls
	/// Used to convert between raw and position
	/// </summary>
	public static class VolumeRawLevelDeviceControlsExtensions
	{
		/// <summary>
		/// Convert from a raw volume to a position value
		/// </summary>
		/// <param name="control"></param>
		/// <param name="volumeRaw">Volume Raw Value</param>
		/// <returns>Volume position, between 0 and 1</returns>
		public static float ConvertLevelToPosition(this IVolumeLevelDeviceControl control, float volumeRaw)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return VolumeUtils.ConvertRawToPosition(control.VolumeLevelMinRange, control.VolumeLevelMaxRange, volumeRaw);
		}

		/// <summary>
		/// Convert from a position value to a raw volume
		/// </summary>
		/// <param name="control"></param>
		/// <param name="volumePosition">Volume Position Value, between 0 and 1</param>
		/// <returns>Volume Raw Value</returns>
		public static float ConvertPositionToLevel(this IVolumeLevelDeviceControl control, float volumePosition)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return VolumeUtils.ConvertPositionToRaw(control.VolumeLevelMinRange, control.VolumeLevelMaxRange, volumePosition);
		}

		public static float ClampToVolumeLevel(this IVolumeLevelDeviceControl control, float level)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return MathUtils.Clamp(level, control.VolumeLevelMinRange, control.VolumeLevelMaxRange);
		}
	}
}