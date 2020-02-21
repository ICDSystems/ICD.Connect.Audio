﻿using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Devices.EventArguments;

namespace ICD.Connect.Audio.Utils
{
	public sealed class VolumePointHelper : IDisposable
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// Will not raise if mute feedback is not supported.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnVolumeControlIsMutedChanged;

		/// <summary>
		/// Raised when the volume changes.
		/// Will not raise if volume feedback is not supported.
		/// </summary>
		public event EventHandler<FloatEventArgs> OnVolumeControlVolumeChanged;

		/// <summary>
		/// Raised when the volume control availability changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnVolumeControlAvailableChanged; 

		private IVolumePoint m_VolumePoint;
		private IVolumeDeviceControl m_VolumeControl;
		private bool m_IsMuted;
		private bool m_ControlAvailable;
		private float m_VolumeLevel;

		#region Properties

		/// <summary>
		/// Gets/sets the volume point.
		/// </summary>
		[CanBeNull]
		public IVolumePoint VolumePoint
		{
			get { return m_VolumePoint; }
			set
			{
				if (value == m_VolumePoint)
					return;

				Unsubscribe(m_VolumePoint);
				m_VolumePoint = value;
				Subscribe(m_VolumePoint);

				UpdateVolumeControl();
			}
		}

		/// <summary>
		/// Gets the volume control for the current volume point.
		/// </summary>
		[CanBeNull]
		public IVolumeDeviceControl VolumeControl
		{
			get { return m_VolumeControl; }
			private set
			{
				if (value == m_VolumeControl)
					return;
				
				Unsubscribe(m_VolumeControl);
				m_VolumeControl = value;
				Subscribe(m_VolumeControl);
			}
		}

		/// <summary>
		/// Returns the features supported by the current volume control.
		/// Returns None if the volume control is null.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures
		{
			get { return VolumeControl == null ? eVolumeFeatures.None : VolumeControl.SupportedVolumeFeatures; }
		}

		/// <summary>
		/// Gets the current control volume.
		/// Will return 0 if the volume control is null.
		/// </summary>
		public float VolumeLevel
		{
			get { return m_VolumeLevel; }
			private set
			{
				if (Math.Abs(value - m_VolumeLevel) < 0.001f)
					return;

				m_VolumeLevel = value;

				OnVolumeControlVolumeChanged.Raise(this, new FloatEventArgs(m_VolumeLevel));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// Will return false if mute feedback is not supported.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			private set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				OnVolumeControlIsMutedChanged.Raise(this, new BoolEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// Gets the control availability state.
		/// Will return false if the volume control is null.
		/// </summary>
		public bool ControlAvailable
		{
			get { return m_ControlAvailable; }
			private set
			{
				if (value == m_ControlAvailable)
					return;

				m_ControlAvailable = value;

				OnVolumeControlAvailableChanged.Raise(this, new BoolEventArgs(m_ControlAvailable));
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnVolumeControlIsMutedChanged = null;
			OnVolumeControlVolumeChanged = null;
			OnVolumeControlAvailableChanged = null;

			// Clears and unsubscribes from the point and control
			VolumePoint = null;
		}

		/// <summary>
		/// Gets the volume percent of the current volume control.
		/// Returns 0 if the current volume control is null.
		/// </summary>
		/// <returns></returns>
		public float GetVolumePercent()
		{
			return VolumeControl == null ? 0 : VolumeControl.GetVolumePercent();
		}

		/// <summary>
		/// Sets the volume percent of the current volume control.
		/// Does nothing if the current volume control is null or does not support volume assignment.
		/// </summary>
		/// <param name="percent"></param>
		/// <returns></returns>
		public void SetVolumePercent(float percent)
		{
			if (VolumeControl != null && SupportedVolumeFeatures.HasFlag(eVolumeFeatures.VolumeAssignment))
				VolumeControl.SetVolumePercent(percent);
		}

		/// <summary>
		/// Toggles the mute state of the current volume control.
		/// Does nothing if the current volume control is null or does not support mute toggle.
		/// </summary>
		public void ToggleIsMuted()
		{
			if (VolumeControl != null && SupportedVolumeFeatures.HasFlag(eVolumeFeatures.Mute))
				VolumeControl.ToggleIsMuted();
		}

		/// <summary>
		/// Sets the mute state of the current volume control.
		/// Does nothing if the current volume control is null or does not support mute assignment.
		/// </summary>
		/// <param name="mute"></param>
		public void SetIsMuted(bool mute)
		{
			if (VolumeControl != null && SupportedVolumeFeatures.HasFlag(eVolumeFeatures.MuteAssignment))
				VolumeControl.SetIsMuted(mute);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the current volume control to match the volume point.
		/// </summary>
		private void UpdateVolumeControl()
		{
			VolumeControl = VolumePoint == null ? null : VolumePoint.Control;

			UpdateVolume();
			UpdateIsMuted();
			UpdateControlAvailable();
		}

		/// <summary>
		/// Updates the volume to match the current volume control.
		/// </summary>
		private void UpdateVolume()
		{
			VolumeLevel = VolumeControl == null ? 0 : VolumeControl.VolumeLevel;
		}

		/// <summary>
		/// Updates the mute state to match the current volume control.
		/// </summary>
		private void UpdateIsMuted()
		{
			IsMuted = VolumeControl != null && VolumeControl.IsMuted;
		}

		/// <summary>
		/// Updates the control available state to match the current volume control.
		/// </summary>
		private void UpdateControlAvailable()
		{
			ControlAvailable = VolumeControl != null && VolumeControl.ControlAvailable;
		}

		#endregion

		#region VolumePoint Callbacks

		/// <summary>
		/// Subscribe to the volume point events.
		/// </summary>
		/// <param name="volumePoint"></param>
		private void Subscribe(IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				return;

			volumePoint.OnControlChanged += VolumePointOnControlChanged;
		}

		/// <summary>
		/// Unsubscribe from the volume point events.
		/// </summary>
		/// <param name="volumePoint"></param>
		private void Unsubscribe(IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				return;

			volumePoint.OnControlChanged -= VolumePointOnControlChanged;
		}

		/// <summary>
		/// Called when the wrapped volume control changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VolumePointOnControlChanged(object sender, DeviceControlEventArgs eventArgs)
		{
			UpdateVolumeControl();
		}

		#endregion

		#region VolumeControl Callbacks

		/// <summary>
		/// Subscribe to the volume control events.
		/// </summary>
		/// <param name="volumeControl"></param>
		private void Subscribe(IVolumeDeviceControl volumeControl)
		{
			if (volumeControl == null)
				return;

			volumeControl.OnIsMutedChanged += VolumeControlOnIsMutedChanged;
			volumeControl.OnVolumeChanged += VolumeControlOnVolumeChanged;
			volumeControl.OnControlAvailableChanged += VolumeControlOnControlAvailableChanged;
		}

		/// <summary>
		/// Unsubscribe from the volume control events.
		/// </summary>
		/// <param name="volumeControl"></param>
		private void Unsubscribe(IVolumeDeviceControl volumeControl)
		{
			if (volumeControl == null)
				return;

			volumeControl.OnIsMutedChanged -= VolumeControlOnIsMutedChanged;
			volumeControl.OnVolumeChanged -= VolumeControlOnVolumeChanged;
			volumeControl.OnControlAvailableChanged -= VolumeControlOnControlAvailableChanged;
		}

		/// <summary>
		/// Called when the current controls volume changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VolumeControlOnVolumeChanged(object sender, VolumeControlVolumeChangedApiEventArgs eventArgs)
		{
			UpdateVolume();
		}

		/// <summary>
		/// Called when the current controls mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VolumeControlOnIsMutedChanged(object sender, VolumeControlIsMutedChangedApiEventArgs eventArgs)
		{
			UpdateIsMuted();
		}

		/// <summary>
		/// Called when the current controls availability state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VolumeControlOnControlAvailableChanged(object sender, DeviceControlAvailableApiEventArgs eventArgs)
		{
			UpdateControlAvailable();
		}

		#endregion
	}
}