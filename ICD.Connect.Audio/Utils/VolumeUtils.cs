using System;
using ICD.Common.Properties;

namespace ICD.Connect.Audio.Utils
{
	public static class VolumeUtils
	{
		/// <summary>
		/// Gets a string for the volume using the given representation.
		/// </summary>
		/// <param name="volume"></param>
		/// <param name="representation"></param>
		/// <returns></returns>
		[NotNull]
		public static string ToString([CanBeNull] float? volume, eVolumeRepresentation representation)
		{
			return volume.HasValue
				? ToString(volume.Value, representation)
				: string.Empty;
		}

		/// <summary>
		/// Gets a string for the volume using the given representation.
		/// </summary>
		/// <param name="volume"></param>
		/// <param name="representation"></param>
		/// <returns></returns>
		[NotNull]
		public static string ToString(float volume, eVolumeRepresentation representation)
		{
			switch (representation)
			{
				case eVolumeRepresentation.Level:
					return string.Format("{0:n2}", volume);
				case eVolumeRepresentation.Percent:
					return string.Format("{0:n2}%", volume * 100.0f);
				default:
					throw new ArgumentOutOfRangeException("representation");
			}
		}
	}
}
