using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Volume;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Controls.Volume
{
	public interface IVolumePercentDeviceControl : IVolumeRampDeviceControl
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		[ApiEvent(VolumeLevelDeviceControlApi.EVENT_VOLUME_CHANGED, VolumeLevelDeviceControlApi.HELP_EVENT_VOLUME_CHANGED)]
		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_VOLUME_CHANGED)]
		event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		#region Properties

		/// <summary>
		/// Gets the current volume percent, 0 - 1
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_PERCENT, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_PERCENT)]
		[DynamicPropertyTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_PERCENT, VolumeTelemetryNames.VOLUME_CONTROL_VOLUME_CHANGED)]
		float VolumePercent { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		[ApiProperty(VolumeLevelDeviceControlApi.PROPERTY_VOLUME_STRING, VolumeLevelDeviceControlApi.HELP_PROPERTY_VOLUME_STRING)]
		string VolumeString { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the volume percent, 0 - 1
		/// </summary>
		/// <param name="percent"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_SET_VOLUME_PERCENT, VolumeLevelDeviceControlApi.HELP_METHOD_SET_VOLUME_PERCENT)]
		[MethodTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_PERCENT_COMMAND)]
		void SetVolumePercent(float percent);

		/// <summary>
		/// Starts raising the volume in steps of the given percent, and continues until RampStop is called.
		/// </summary>
		/// <param name="increment"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_PERCENT_UP, VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_PERCENT_UP)]
		void VolumePercentRampUp(float increment);

		/// <summary>
		/// Starts lowering the volume in steps of the given percent, and continues until RampStop is called.
		/// </summary>
		/// <param name="decrement"></param>
		[ApiMethod(VolumeLevelDeviceControlApi.METHOD_VOLUME_LEVEL_RAMP_PERCENT_DOWN, VolumeLevelDeviceControlApi.HELP_METHOD_VOLUME_LEVEL_RAMP_PERCENT_DOWN)]
		void VolumePercentRampDown(float decrement);

		#endregion
	}
}
