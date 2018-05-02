using System;
using ICD.Common.Properties;
using ICD.Common.Utils;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// IVolumeLevelDeviceControl is for devices that have more advanced volume control
	/// For devices that support direct volume setting and volume level feedback.
	/// </summary>
	public interface IVolumeLevelDeviceControl : IVolumeLevelBasicDeviceControl
	{
		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		#region Properties

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		[PublicAPI]
		float VolumeRaw { get; }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		[PublicAPI]
		float VolumePosition { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		[PublicAPI]
		string VolumeString { get; }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		[PublicAPI]
		float? VolumeRawMax { get; }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		[PublicAPI]
		float? VolumeRawMin { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		[PublicAPI]
		void SetVolumeRaw(float volume);

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		[PublicAPI]
		void SetVolumePosition(float position);

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		[PublicAPI]
		void VolumeLevelIncrement(float incrementValue);

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		[PublicAPI]
		void VolumeLevelDecrement(float decrementValue);

		#endregion
	}

	public static class VolumeLevelDeviceExtensions
	{
		/// <summary>
		/// Gets the clamped value of the level from potential min/max set on the device
		/// </summary>
		/// <param name="control">Volume Control to get clamped value for</param>
		/// <param name="level">Level to clamp</param>
		/// <returns></returns>
		public static float ClampRawVolume(this IVolumeLevelDeviceControl control, float level)
		{
			if (control.VolumeRawMax != null && control.VolumeRawMin != null)
				return MathUtils.Clamp(level, (float)control.VolumeRawMin, (float)control.VolumeRawMax);
			if (control.VolumeRawMin != null)
				return Math.Max(level, (float)control.VolumeRawMin);
			if (control.VolumeRawMax != null)
				return Math.Min(level, (float)control.VolumeRawMax);

			return level;
		}
	}
}
