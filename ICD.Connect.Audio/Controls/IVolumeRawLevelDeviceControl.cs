using System;
using ICD.Common.Utils;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// IVolumeRawLevelDeviceControl is for devices that offer raw volume feedback and not position
	/// Volume Min/Max Range can be used to caculate position
	/// This interface should not be used to refer to volume controls, use IVolumeLevelDeviceControl instead
	/// </summary>
	public interface IVolumeRawLevelDeviceControl : IVolumeLevelDeviceControl
	{
		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		float VolumeRawMaxRange { get; }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		float VolumeRawMinRange { get; }
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
		public static float ConvertRawToPosition(this IVolumeRawLevelDeviceControl control, float volumeRaw)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			if (control.VolumeRawMinRange.Equals(control.VolumeRawMaxRange))
				return 0.0f;

			return MathUtils.MapRange(control.VolumeRawMinRange, control.VolumeRawMaxRange, 0.0f, 1.0f, volumeRaw);
		}

		/// <summary>
		/// Convert from a position value to a raw volume
		/// </summary>
		/// <param name="control"></param>
		/// <param name="volumePosition">Volume Position Value, between 0 and 1</param>
		/// <returns>Volume Raw Value</returns>
		public static float ConvertPositionToRaw(this IVolumeRawLevelDeviceControl control, float volumePosition)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return MathUtils.MapRange(0.0f, 1.0f, control.VolumeRawMinRange, control.VolumeRawMaxRange, volumePosition);
		}

		public static float ClampRawVolume(this IVolumeRawLevelDeviceControl control, float level)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return MathUtils.Clamp(level, control.VolumeRawMinRange, control.VolumeRawMaxRange);
		}
	}
}