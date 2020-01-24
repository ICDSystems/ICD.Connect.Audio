using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Utils;
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
		public eVolumeType VolumeType { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractVolumePoint()
		{
			VolumeRepresentation = eVolumeRepresentation.Percent;
			VolumeSafetyMin = null;
			VolumeSafetyMax = null;
			VolumeDefault = null;
			VolumeRampStepSize = AbstractVolumePointSettings.DEFAULT_STEP_PERCENT;
			VolumeRampInitialStepSize = AbstractVolumePointSettings.DEFAULT_STEP_PERCENT;
			VolumeRampInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
		}

		#region Private Methods

		/// <summary>
		/// Applies the configured default volume to the control.
		/// Does nothing if no default is specified or the control does not support volume assignment.
		/// </summary>
		private void SetDefaultVolume()
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
					Control.SetVolumePercent(VolumeDefault.Value);
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
					if (VolumeSafetyMin != null && Control.GetVolumePercent() < VolumeSafetyMin)
						Control.SetVolumePercent(VolumeSafetyMin.Value);
					else if (VolumeSafetyMax != null && Control.GetVolumePercent() < VolumeSafetyMax)
						Control.SetVolumePercent(VolumeSafetyMax.Value);
					break;

				case eVolumeRepresentation.Level:
					if (VolumeSafetyMin != null && Control.VolumeLevel < VolumeSafetyMin)
						Control.SetVolumeLevel(VolumeSafetyMin.Value);
					else if (VolumeSafetyMax != null && Control.VolumeLevel < VolumeSafetyMax)
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
		private void ControlOnControlAvailableChanged(object sender, DeviceControlAvailableApiEventArgs eventArgs)
		{
			if (eventArgs.Data)
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
			settings.VolumeType = VolumeType;
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
			VolumeRampStepSize = AbstractVolumePointSettings.DEFAULT_STEP_PERCENT;
			VolumeRampInitialStepSize = AbstractVolumePointSettings.DEFAULT_STEP_PERCENT;
			VolumeRampInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = AbstractVolumePointSettings.DEFAULT_STEP_INTERVAL;
			VolumeType = default(eVolumeType);
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
			VolumeType = settings.VolumeType;
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
			addRow("Volume Safety Min", VolumeUtils.ToString(VolumeSafetyMin, VolumeRepresentation));
			addRow("Volume Safety Max", VolumeUtils.ToString(VolumeSafetyMax, VolumeRepresentation));
			addRow("Volume Default", VolumeUtils.ToString(VolumeDefault, VolumeRepresentation));
			addRow("Volume Ramp Step Size", VolumeUtils.ToString(VolumeRampStepSize, VolumeRepresentation));
			addRow("Volume Ramp Initial Step Size", VolumeUtils.ToString(VolumeRampInitialStepSize, VolumeRepresentation));
			addRow("Volume Ramp Interval", VolumeRampInterval + "ms");
			addRow("Volume Ramp Initial Interval", VolumeRampInitialInterval + "ms");
			addRow("Volume Type", VolumeType);
		}

		#endregion
	}
}
