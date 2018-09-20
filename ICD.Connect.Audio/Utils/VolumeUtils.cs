using System;
using ICD.Common.Utils;

namespace ICD.Connect.Audio.Utils
{
	public static class VolumeUtils
	{
		/// <summary>
		/// Convert from a raw volume to a position value.
		/// </summary>
		/// <param name="minRaw"></param>
		/// <param name="maxRaw"></param>
		/// <param name="raw">Volume Raw Value</param>
		/// <returns>Volume position, between 0 and 1</returns>
		public static float ConvertRawToPosition(float minRaw, float maxRaw, float raw)
		{
			if (Math.Abs(minRaw - maxRaw) < 0.0001f)
				return 0.0f;

			return MathUtils.MapRange(minRaw, maxRaw, 0.0f, 1.0f, raw);
		}

		/// <summary>
		/// Convert from a position value to a raw volume.
		/// </summary>
		/// <param name="minRaw"></param>
		/// <param name="maxRaw"></param>
		/// <param name="position">Volume Position Value, between 0 and 1</param>
		/// <returns>Volume Raw Value</returns>
		public static float ConvertPositionToRaw(float minRaw, float maxRaw, float position)
		{
			return MathUtils.MapRange(0.0f, 1.0f, minRaw, maxRaw, position);
		}

		/// <summary>
		/// Gets the clamped value of the level from potential min/max set on the device
		/// </summary>
		/// <param name="minRaw"></param>
		/// <param name="maxRaw"></param>
		/// <param name="raw">Level to clamp</param>
		/// <returns></returns>
		public static float ClampRawVolume(float? minRaw, float? maxRaw, float raw)
		{
			if (maxRaw != null && minRaw != null)
				return MathUtils.Clamp(raw, (float)minRaw, (float)maxRaw);
			if (minRaw != null)
				return Math.Max(raw, (float)minRaw);
			if (maxRaw != null)
				return Math.Min(raw, (float)maxRaw);

			return raw;
		}
	}
}
