using System;
using System.Collections.Generic;
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
		private const int PRESET_HOME_ID = 1;
		private const int PRESET_PRIVACY_ID = 2;

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

		// Used with settings
		private string m_ComponentName;

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
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PAN_LEFT);
					break;
				case eCameraPanTiltAction.Right:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PAN_RIGHT);
					break;
				case eCameraPanTiltAction.Up:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_TILT_UP);
					break;
				case eCameraPanTiltAction.Down:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_TILT_DOWN);
					break;
				case eCameraPanTiltAction.Stop:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PAN_CURRENT);
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_TILT_CURRENT);
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
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_ZOOM_IN);
					break;
				case eCameraZoomAction.ZoomOut:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_ZOOM_OUT);
					break;
				case eCameraZoomAction.Stop:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_ZOOM_CURRENT);
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
		public int MaxPresets { get { return 2; } }

		/// <summary>
		/// Gets the stored camera presets.
		/// </summary>
		public IEnumerable<CameraPreset> GetPresets()
		{
			yield return new CameraPreset(PRESET_HOME_ID, "Home");
			yield return new CameraPreset(PRESET_PRIVACY_ID, "Privacy");
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public void ActivatePreset(int presetId)
		{
			if (m_CameraComponent == null)
				return;

			switch (presetId)
			{
				case PRESET_HOME_ID:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_HOME_LOAD_TRIGGER);
					break;

				case PRESET_PRIVACY_ID:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_PRIVATE_LOAD_TRIGGER);
					break;

				default:
					Log(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not activated.", MaxPresets);
					return;
			}
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public void StorePreset(int presetId)
		{
			if (m_CameraComponent == null)
				return;

			switch (presetId)
			{
				case PRESET_HOME_ID:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_HOME_SAVE_TRIGGER);
					break;

				case PRESET_PRIVACY_ID:
					m_CameraComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_PRIVATE_SAVE_TRIGGER);
					break;

				default:
					Log(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
					return;
			}
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

			m_ComponentName = null;
			SetDsp(null, null);
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

			SetDsp(codec, settings.ComponentName);
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
		}

		#endregion

		#region Public API

		[PublicAPI]
		public void SetDsp(QSysCoreDevice dsp, string componentName)
		{
			if (dsp == m_Dsp)
				return;

			Unsubscribe(m_Dsp);

			m_Dsp = dsp;
			m_ComponentName = componentName;

			m_CameraComponent =
				m_Dsp == null
					? null
					: m_Dsp.Components.LazyLoadNamedComponent<CameraNamedComponent>(m_ComponentName);

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