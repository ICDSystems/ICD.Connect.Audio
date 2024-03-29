﻿using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Devices.Points;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.VolumePoints
{
	public abstract class AbstractVolumePoint<TSettings> : AbstractPoint<TSettings, IVolumeDeviceControl>, IVolumePoint
		where TSettings : IVolumePointSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets the category for this originator type (e.g. Device, Port, etc)
		/// </summary>
		public override string Category { get { return "VolumePoint"; } }

		/// <summary>
		/// Determines how the volume levels and ramping are defined for this volume point.
		/// </summary>
		public eVolumeRepresentation VolumeRepresentation { get; set; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		public float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		public float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		public float? VolumeDefault { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for each ramp interval.
		/// </summary>
		public float VolumeRampStepSize { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for the first ramp interval.
		/// </summary>
		public float VolumeRampInitialStepSize { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between each volume ramp step.
		/// </summary>
		public long VolumeRampInterval { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between the first and second ramp step.
		/// </summary>
		public long VolumeRampInitialInterval { get; set; }

		/// <summary>
		/// Determines when this control is used contextually.
		/// </summary>
		public eVolumePointContext Context { get; set; }

		/// <summary>
		/// Determines what muting this volume point will do (mute audio output, mute microphones, etc).
		/// </summary>
		public eMuteType MuteType { get; set; }

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		public ePrivacyMuteFeedback PrivacyMuteMask { get; set; }

		/// <summary>
		/// If enabled, prevents default volume from getting set on the control automatically
		/// Specific implementaitons may set default volume under other conditions
		/// </summary>
		public bool InhibitAutoDefaultVolume { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractVolumePoint()
		{
			VolumeRepresentation = eVolumeRepresentation.Percent;
			VolumeRampStepSize = AbstractVolumePointSettings.DEFAULT_STEP;
			VolumeRampInitialStepSize = AbstractVolumePointSettings.DEFAULT_STEP;
			VolumeRampInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
		}

		#region Private Methods

		/// <summary>
		/// Applies the configured default volume to the control.
		/// Does nothing if no default is specified or the control does not support volume assignment.
		/// </summary>
		protected void SetDefaultVolume()
		{
			// Point isn't configured for it
			if (VolumeDefault == null)
				return;

			// Control doesn't support it
			if (Control == null || !Control.SupportedVolumeFeatures.HasFlag(eVolumeFeatures.VolumeAssignment))
				return;

			switch (VolumeRepresentation)
			{
				case eVolumeRepresentation.Percent:
					Control.SetVolumePercent(VolumeDefault.Value / 100);
					break;

				case eVolumeRepresentation.Level:
					Control.SetVolumeLevel(VolumeDefault.Value);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Sets the volume on the control back within safety ranges.
		/// Does nothing if no safety range is specified or the control does not support volume assignment.
		/// </summary>
		private void ClampSafetyVolume()
		{
			// Control doesn't support it
			if (Control == null || !Control.SupportedVolumeFeatures.HasFlag(eVolumeFeatures.VolumeAssignment))
				return;

			switch (VolumeRepresentation)
			{
				case eVolumeRepresentation.Percent:
					if (VolumeSafetyMin != null && Control.GetVolumePercent() < VolumeSafetyMin / 100)
						Control.SetVolumePercent(VolumeSafetyMin.Value / 100);
					else if (VolumeSafetyMax != null && Control.GetVolumePercent() > VolumeSafetyMax / 100)
						Control.SetVolumePercent(VolumeSafetyMax.Value / 100);
					break;

				case eVolumeRepresentation.Level:
					if (VolumeSafetyMin != null && Control.VolumeLevel < VolumeSafetyMin)
						Control.SetVolumeLevel(VolumeSafetyMin.Value);
					else if (VolumeSafetyMax != null && Control.VolumeLevel > VolumeSafetyMax)
						Control.SetVolumeLevel(VolumeSafetyMax.Value);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Control Callbacks

		/// <summary>
		/// Override to handle unsubscribing to the control events.
		/// </summary>
		/// <param name="control"></param>
		protected override void Subscribe(IVolumeDeviceControl control)
		{
			base.Subscribe(control);

			if (control == null)
				return;

			control.OnControlAvailableChanged += ControlOnControlAvailableChanged;
			control.OnVolumeChanged += ControlOnVolumeChanged;
		}

		/// <summary>
		/// Override to handle unsubscribing from the control events.
		/// </summary>
		/// <param name="control"></param>
		protected override void Unsubscribe(IVolumeDeviceControl control)
		{
			base.Unsubscribe(control);

			if (control == null)
				return;

			control.OnControlAvailableChanged -= ControlOnControlAvailableChanged;
			control.OnVolumeChanged -= ControlOnVolumeChanged;
		}

		/// <summary>
		/// Called when control availability changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected virtual void ControlOnControlAvailableChanged(object sender, DeviceControlAvailableApiEventArgs eventArgs)
		{
			if (eventArgs.Data && !InhibitAutoDefaultVolume)
				SetDefaultVolume();
		}

		/// <summary>
		/// Called when control volume changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ControlOnVolumeChanged(object sender, VolumeControlVolumeChangedApiEventArgs eventArgs)
		{
			ClampSafetyVolume();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.VolumeRepresentation = VolumeRepresentation;
			settings.VolumeSafetyMin = VolumeSafetyMin;
			settings.VolumeSafetyMax = VolumeSafetyMax;
			settings.VolumeDefault = VolumeDefault;
			settings.VolumeRampStepSize = VolumeRampStepSize;
			settings.VolumeRampInitialStepSize = VolumeRampInitialStepSize;
			settings.VolumeRampInterval = VolumeRampInterval;
			settings.VolumeRampInitialInterval = VolumeRampInitialInterval;
			settings.Context = Context;
			settings.MuteType = MuteType;
			settings.PrivacyMuteMask = PrivacyMuteMask;
			settings.InhibitAutoDefaultVolume = InhibitAutoDefaultVolume;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			VolumeRepresentation = eVolumeRepresentation.Percent;
			VolumeSafetyMin = null;
			VolumeSafetyMax = null;
			VolumeDefault = null;
			VolumeRampStepSize = AbstractVolumePointSettings.DEFAULT_STEP;
			VolumeRampInitialStepSize = AbstractVolumePointSettings.DEFAULT_STEP;
			VolumeRampInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			Context = default(eVolumePointContext);
			MuteType = default(eMuteType);
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
			InhibitAutoDefaultVolume = false;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			VolumeRepresentation = settings.VolumeRepresentation;
			VolumeSafetyMin = settings.VolumeSafetyMin;
			VolumeSafetyMax = settings.VolumeSafetyMax;
			VolumeDefault = settings.VolumeDefault;
			VolumeRampStepSize = settings.VolumeRampStepSize;
			VolumeRampInitialStepSize = settings.VolumeRampInitialStepSize;
			VolumeRampInterval = settings.VolumeRampInterval;
			VolumeRampInitialInterval = settings.VolumeRampInitialInterval;
			Context = settings.Context;
			MuteType = settings.MuteType;
			PrivacyMuteMask = settings.PrivacyMuteMask;
			InhibitAutoDefaultVolume = settings.InhibitAutoDefaultVolume;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Volume Range Mode", VolumeRepresentation);
			addRow("Volume Safety Min", ToString(VolumeSafetyMin, VolumeRepresentation));
			addRow("Volume Safety Max", ToString(VolumeSafetyMax, VolumeRepresentation));
			addRow("Volume Default", ToString(VolumeDefault, VolumeRepresentation));
			addRow("Volume Ramp Step Size", ToString(VolumeRampStepSize, VolumeRepresentation));
			addRow("Volume Ramp Initial Step Size", ToString(VolumeRampInitialStepSize, VolumeRepresentation));
			addRow("Volume Ramp Interval", VolumeRampInterval + "ms");
			addRow("Volume Ramp Initial Interval", VolumeRampInitialInterval + "ms");
			addRow("Context", Context);
			addRow("Mute Type", MuteType);
			addRow("Privacy Mute Mask", PrivacyMuteMask);
			addRow("Inhibit Auto Default", InhibitAutoDefaultVolume);
		}

		/// <summary>
		/// Gets a string for the volume using the given representation.
		/// </summary>
		/// <param name="volume"></param>
		/// <param name="representation"></param>
		/// <returns></returns>
		private static string ToString(float? volume, eVolumeRepresentation representation)
		{
			if (!volume.HasValue)
				return string.Empty;

			switch (representation)
			{
				case eVolumeRepresentation.Level:
					return string.Format("{0:n2}", volume.Value);
				case eVolumeRepresentation.Percent:
					return string.Format("{0:n2}%", volume.Value);
				default:
					throw new ArgumentOutOfRangeException("representation");
			}
		}

		#endregion
	}
}
