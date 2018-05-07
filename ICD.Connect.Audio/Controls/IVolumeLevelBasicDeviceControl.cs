using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// Basic volume control for devices
	/// Supports Up/Down
	/// Designed to support IR controlled devices
	/// </summary>
	public interface IVolumeLevelBasicDeviceControl : IVolumeDeviceControl
	{
		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeLevelBasicDeviceControlApi.METHOD_VOLUME_LEVEL_INCREMENT,
			VolumeLevelBasicDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_INCREMENT)]
		void VolumeLevelIncrement();

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		[ApiMethod(VolumeLevelBasicDeviceControlApi.METHOD_VOLUME_LEVEL_DECREMENT,
			VolumeLevelBasicDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_DECREMENT)]
		void VolumeLevelDecrement();

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		[ApiMethod(VolumeLevelBasicDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_UP,
			VolumeLevelBasicDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_UP)]
		void VolumeLevelRampUp();

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		[ApiMethod(VolumeLevelBasicDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_DOWN,
			VolumeLevelBasicDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_DOWN)]
		void VolumeLevelRampDown();

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		[ApiMethod(VolumeLevelBasicDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_STOP,
		VolumeLevelBasicDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_STOP)]
		void VolumeLevelRampStop();
	}
}
