using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.QSys.Devices.QSysCore;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.QSys.Devices.QSysCoreCamera
{
	public sealed class QSysCoreCameraDevice : AbstractCameraDevice<QSysCoreCameraDeviceSettings>, ICameraWithPanTilt, ICameraWithZoom, ICameraWithPresets
	{
		/// <summary>
		/// Raised when the parent DSP changes.
		/// </summary>
		public event EventHandler OnDspChanged;

		/// <summary>
		/// Raised when the presets are changed.
		/// </summary>
		public event EventHandler OnPresetsChanged;

		[CanBeNull]
		private QSysCoreDevice m_Dsp;

		[CanBeNull]
		private CameraNamedComponent m_CameraComponent;

		[CanBeNull]
		private SnapshotNamedComponent m_SnapshotsComponent;

		// Used with settings
		private string m_ComponentName;
		private string m_SnapshotsName;

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreCameraDevice()
		{
			Controls.Add(new GenericCameraRouteSourceControl<QSysCoreCameraDevice>(this, 0));
			Controls.Add(new PanTiltControl<QSysCoreCameraDevice>(this, 1));
			Controls.Add(new ZoomControl<QSysCoreCameraDevice>(this, 2));
			Controls.Add(new PresetControl<QSysCoreCameraDevice>(this, 3));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnDspChanged = null;
			OnPresetsChanged = null;

			base.DisposeFinal(disposing);
		}

		#region ICameraWithPanTilt

		/// <summary>
		/// Starts rotating the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void PanTilt(eCameraPanTiltAction action)
		{
			if (m_CameraComponent == null)
				return;

			switch (action)
			{
				case eCameraPanTiltAction.Left:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_PAN_LEFT, "1");
					break;
				case eCameraPanTiltAction.Right:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_PAN_RIGHT, "1");
					break;
				case eCameraPanTiltAction.Up:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_TILT_UP, "1");
					break;
				case eCameraPanTiltAction.Down:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_TILT_DOWN, "1");
					break;
				case eCameraPanTiltAction.Stop:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_PAN_LEFT, "0");
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_PAN_RIGHT, "0");
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_TILT_UP, "0");
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_TILT_DOWN, "0");
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region ICameraWithZoom

		/// <summary>
		/// Starts zooming the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void Zoom(eCameraZoomAction action)
		{
			if (m_CameraComponent == null)
				return;

			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_IN, "1");
					break;
				case eCameraZoomAction.ZoomOut:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_OUT, "1");
					break;
				case eCameraZoomAction.Stop:
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_IN, "0");
					m_CameraComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_OUT, "0");
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region ICameraWithPresets

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public int MaxPresets { get { return m_SnapshotsComponent == null ? 0 : m_SnapshotsComponent.SnapshotCount; } }

		/// <summary>
		/// Gets the stored camera presets.
		/// </summary>
		public IEnumerable<CameraPreset> GetPresets()
		{
			return m_SnapshotsComponent == null
				       ? Enumerable.Empty<CameraPreset>()
				       : Enumerable.Range(1, m_SnapshotsComponent.SnapshotCount)
				                   .Select(i => new CameraPreset(i, "Preset " + i));
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public void ActivatePreset(int presetId)
		{
			if (m_SnapshotsComponent == null)
				return;

			m_SnapshotsComponent.LoadSnapshot(presetId);
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public void StorePreset(int presetId)
		{
			if (m_SnapshotsComponent == null)
				return;

			m_SnapshotsComponent.SaveSnapshot(presetId);
		}

		#endregion

		#region DeviceBase

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Dsp != null && m_Dsp.IsOnline;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetDsp(null, null, null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(QSysCoreCameraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			QSysCoreDevice codec = null;

			if (settings.DspId.HasValue)
			{
				try
				{
					codec = factory.GetOriginatorById<QSysCoreDevice>(settings.DspId.Value);
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No QSys Core Device with id {0}", settings.DspId.Value);
				}
			}

			SetDsp(codec, settings.ComponentName, settings.SnapshotsName);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(QSysCoreCameraDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.DspId = m_Dsp == null ? null : (int?)m_Dsp.Id;
			settings.ComponentName = m_ComponentName;
			settings.SnapshotsName = m_SnapshotsName;
		}

		#endregion

		#region Public API

		[PublicAPI]
		public void SetDsp(QSysCoreDevice dsp, string componentName, string snapshotsName)
		{
			Unsubscribe(m_Dsp);

			m_Dsp = dsp;
			m_ComponentName = componentName;
			m_SnapshotsName = snapshotsName;

			m_CameraComponent =
				m_Dsp == null || m_ComponentName == null
					? null
					: m_Dsp.Components.LazyLoadNamedComponent<CameraNamedComponent>(m_ComponentName);

			m_SnapshotsComponent =
				m_Dsp == null || m_SnapshotsName == null
					? null
					: m_Dsp.Components.LazyLoadNamedComponent<SnapshotNamedComponent>(m_SnapshotsName);

			Subscribe(m_Dsp);

			UpdateCachedOnlineStatus();

			OnDspChanged.Raise(this);
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribe to the dsp events.
		/// </summary>
		/// <param name="dsp"></param>
		private void Subscribe(QSysCoreDevice dsp)
		{
			if (dsp == null)
				return;

			dsp.OnIsOnlineStateChanged += DspOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the dsp events.
		/// </summary>
		/// <param name="dsp"></param>
		private void Unsubscribe(QSysCoreDevice dsp)
		{
			if (dsp == null)
				return;

			dsp.OnIsOnlineStateChanged -= DspOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the dsp changes online status.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void DspOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion
	}
}