using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Volume;

namespace ICD.Connect.Audio.Controls.Volume
{
	/// <summary>
	/// IVolumeLevelDeviceControl is for devices that have more advanced volume control
	/// For devices that support direct volume setting and volume level feedback.
	/// </summary>
	public interface IVolumePositionDeviceControl : IVolumeRampDeviceControl
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		[ApiEvent(VolumeLevelDeviceControlApi.EVENT_VOLUME_CHANGED, VolumeLevelDeviceControlApi.HELP_EVENT_VOLUME_CHANGED)]
		event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		#region Properties

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

		#endregion

		#region Methods

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_POSITION,
			VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_POSITION)]
		void SetVolumePosition(float position);

		/// <summary>
		/// Starts raising the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="increment"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_POSITION_UP,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_POSITION_UP)]
		void VolumePositionRampUp(float increment);

		/// <summary>
		/// Starts lowering the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="decrement"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_POSITION_DOWN,
			VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_POSITION_DOWN)]
		void VolumePositionRampDown(float decrement);

		#endregion
	}
}
