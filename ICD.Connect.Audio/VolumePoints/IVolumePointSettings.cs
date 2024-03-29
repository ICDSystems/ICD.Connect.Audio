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
		/// Determines the contextual availability of this volume point.
		/// </summary>
		eVolumePointContext Context { get; set; }

		/// <summary>
		/// Determines what muting this volume point will do (mute audio output, mute microphones, etc).
		/// </summary>
		eMuteType MuteType { get; set; }

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		ePrivacyMuteFeedback PrivacyMuteMask { get; set; }

		/// <summary>
		/// If enabled, prevents default volume from getting set on the control automatically
		/// Specific implementaitons may set default volume under other conditions
		/// </summary>
		bool InhibitAutoDefaultVolume { get; set; }
	}
}
