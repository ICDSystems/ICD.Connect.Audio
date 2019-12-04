using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls.Volume;

namespace ICD.Connect.Audio.Controls.Volume
{
	public interface IVolumeLevelDeviceControl : IVolumePercentDeviceControl
	{
		/// <summary>
		/// Gets the current volume in the range VolumeLevelMin to VolumeLevelMax.
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL)]
		float VolumeLevel { get; }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL_MIN, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MIN)]
		float VolumeLevelMin { get; }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_LEVEL_MAX, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_LEVEL_MAX)]
		float VolumeLevelMax { get; }

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_LEVEL, VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_LEVEL)]
		void SetVolumeLevel(float volume);
	}

	public static class VolumeLevelDeviceControlExtensions
	{
		/// <summary>
		/// Increments the volume.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="incrementValue"></param>
		public static void VolumeLevelIncrement([NotNull] this IVolumeLevelDeviceControl extends, float incrementValue)
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
		public static void VolumeLevelDecrement([NotNull] this IVolumeLevelDeviceControl extends, float decrementValue)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			float newLevel = extends.VolumeLevel - decrementValue;
			extends.SetVolumeLevel(newLevel);
		}

		/// <summary>
		/// Convert from a volume level to a percentage.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="level">Volume Level</param>
		/// <returns>Volume percent, between 0 and 1</returns>
		public static float ConvertLevelToPercent([NotNull] this IVolumeLevelDeviceControl extends, float level)
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
		public static float ConvertPercentToLevel([NotNull] this IVolumeLevelDeviceControl extends, float percent)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return MathUtils.FromPercent(extends.VolumeLevelMin, extends.VolumeLevelMax, percent);
		}

		/// <summary>
		/// Clamps the given volume level to the min/max of the control.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		public static float ClampToVolumeLevel([NotNull] this IVolumeLevelDeviceControl extends, float level)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return MathUtils.Clamp(level, extends.VolumeLevelMin, extends.VolumeLevelMax);
		}
	}
}