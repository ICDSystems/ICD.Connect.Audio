using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Proxies.Controls;
using ICD.Connect.Audio.Utils;

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
		[ApiEvent(VolumeLevelDeviceControlApi.EVENT_VOLUME_CHANGED, VolumeLevelDeviceControlApi.HELP_EVENT_VOLUME_CHANGED)]
		event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		#region Properties

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_RAW, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_RAW)]
		float VolumeRaw { get; }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_POSITION,
			VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_POSITION)]
		float VolumePosition { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_STRING,
			VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_STRING)]
		string VolumeString { get; }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_RAW_MAX,
			VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_RAW_MAX)]
		float? VolumeRawMax { get; }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_RAW_MIN,
			VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_RAW_MIN)]
		float? VolumeRawMin { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_RAW, VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_RAW)]
		void SetVolumeRaw(float volume);

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_POSITION,
			VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_POSITION)]
		void SetVolumePosition(float position);

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_INCREMENT,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_INCREMENT)]
		void VolumeLevelIncrement(float incrementValue);

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_DECREMENT,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_DECREMENT)]
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
			if (control == null)
				throw new ArgumentNullException("control");

			return VolumeUtils.ClampRawVolume(control.VolumeRawMin, control.VolumeRawMax, level);
		}
	}
}
