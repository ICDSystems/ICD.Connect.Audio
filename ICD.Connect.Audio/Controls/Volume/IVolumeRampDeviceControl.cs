using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// Basic volume control for devices
	/// Supports Up/Down
	/// Designed to support IR controlled devices
	/// </summary>
	public interface IVolumeRampDeviceControl : IVolumeDeviceControl
	{
		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeRampDeviceControlApi.METHOD_VOLUME_INCREMENT,
			VolumeRampDeviceControlApi.HELP_METHOD_VOLUME_INCREMENT)]
		void VolumeIncrement();

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeRampDeviceControlApi.METHOD_VOLUME_DECREMENT,
			VolumeRampDeviceControlApi.HELP_METHOD_VOLUME_DECREMENT)]
		void VolumeDecrement();

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeRampStop"/> must be called after
		/// </summary>
		[ApiMethod(VolumeRampDeviceControlApi.METHOD_VOLUME_RAMP_UP,
			VolumeRampDeviceControlApi.HELP_METHOD_VOLUME_RAMP_UP)]
		void VolumeRampUp();

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeRampStop"/> must be called after
		/// </summary>
		[ApiMethod(VolumeRampDeviceControlApi.METHOD_VOLUME_RAMP_DOWN,
			VolumeRampDeviceControlApi.HELP_METHOD_VOLUME_RAMP_DOWN)]
		void VolumeRampDown();

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		[ApiMethod(VolumeRampDeviceControlApi.METHOD_VOLUME_RAMP_STOP,
		VolumeRampDeviceControlApi.HELP_METHOD_VOLUME_RAMP_STOP)]
		void VolumeRampStop();
	}
}
