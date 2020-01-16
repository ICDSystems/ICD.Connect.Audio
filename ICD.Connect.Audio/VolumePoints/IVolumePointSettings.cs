﻿using ICD.Connect.Devices.Points;

namespace ICD.Connect.Audio.VolumePoints
{
	public interface IVolumePointSettings : IPointSettings
	{
		/// <summary>
		/// Determines how the volume safety min, max and default are defined.
		/// </summary>
		eVolumeRepresentation VolumeRepresentation { get; set; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		float? VolumeDefault { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for each ramp interval.
		/// </summary>
		float VolumeRampStepSize { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for the first ramp interval.
		/// </summary>
		float VolumeRampInitialStepSize { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between each volume ramp step.
		/// </summary>
		long VolumeRampInterval { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between the first and second ramp step.
		/// </summary>
		long VolumeRampInitialInterval { get; set; }

		/// <summary>
		/// Gets/sets the context for the volume point.
		/// </summary>
		eVolumeType VolumeType { get; set; }
	}
}
