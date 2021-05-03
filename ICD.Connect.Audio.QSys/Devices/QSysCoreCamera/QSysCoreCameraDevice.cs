using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.EventArgs;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.QSys.Devices.QSysCoreCamera
{
	

	public sealed class QSysCoreCameraDevice : AbstractNamedComponentQSysDevice<QSysCoreCameraDeviceSettings, CameraNamedComponent>, ICameraDevice
	{
		private const float TOLERANCE = 0.0001f;

		[CanBeNull]
		private SnapshotNamedComponent m_SnapshotsComponent;

		public string SnapshotComponentName { get; private set; }
		private eCameraFeatures m_SupportedCameraFeatures;
		private bool m_IsCameraMuted;

		public SnapshotNamedComponent SnapshotsComponent
		{
			get { return m_SnapshotsComponent; } 
			private set
			{
				if (m_SnapshotsComponent == value)
					return;

				m_SnapshotsComponent = value;

				RaisePresetsChanged();
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreCameraDevice()
		{
			SupportedCameraFeatures =
				eCameraFeatures.PanTiltZoom |
				eCameraFeatures.Presets |
				eCameraFeatures.Home |
				eCameraFeatures.Mute;
		}

		protected override void UpdateDspControls()
		{
			base.UpdateDspControls();

			SnapshotsComponent =
				Dsp == null || SnapshotComponentName == null
					? null
					: Dsp.Components.LazyLoadNamedComponent<SnapshotNamedComponent>(SnapshotComponentName);
		}

		/// <summary>
		/// Raised the presets changed event.
		/// </summary>
		private void RaisePresetsChanged()
		{
			IEnumerable<CameraPreset> data = GetPresets();
			OnPresetsChanged.Raise(this, data);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			SnapshotComponentName = null;
			SnapshotsComponent = null;

			OnPresetsChanged = null;
			OnSupportedCameraFeaturesChanged = null;
			OnCameraMuteStateChanged = null;
		}

		#region ICameraDevice

		/// <summary>
		/// Raised when the collection of presets is modified
		/// </summary>
		public event EventHandler<GenericEventArgs<IEnumerable<CameraPreset>>> OnPresetsChanged;

		/// <summary>
		/// Raised when the supported features list is updated
		/// </summary>
		public event EventHandler<GenericEventArgs<eCameraFeatures>> OnSupportedCameraFeaturesChanged;

		/// <summary>
		/// Raised when the mute state changes on the camera.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCameraMuteStateChanged;

		/// <summary>
		/// Gets whether the camera is currently muted
		/// </summary>
		public bool IsCameraMuted
		{
			get { return m_IsCameraMuted; } 
			private set
			{
				if (m_IsCameraMuted == value)
					return;

				m_IsCameraMuted = value;

				OnCameraMuteStateChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public int MaxPresets { get { return m_SnapshotsComponent == null ? 0 : m_SnapshotsComponent.SnapshotCount; } }

		/// <summary>
		/// Flags which indicate which features this camera can support
		/// </summary>
		public eCameraFeatures SupportedCameraFeatures
		{
			get { return m_SupportedCameraFeatures; } 
			private set
			{
				if (m_SupportedCameraFeatures == value)
					return;

				m_SupportedCameraFeatures = value;

				OnSupportedCameraFeaturesChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Begins panning the camera
		/// </summary>
		/// <param name="action"></param>
		public void Pan(eCameraPanAction action)
		{
			if (NamedComponent == null)
				return;

			switch (action)
			{
				case eCameraPanAction.Left:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_PAN_LEFT, "1");
					break;
				case eCameraPanAction.Right:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_PAN_RIGHT, "1");
					break;
				case eCameraPanAction.Stop:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_PAN_LEFT, "0");
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_PAN_RIGHT, "0");
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Begin tilting the camera.
		/// </summary>
		public void Tilt(eCameraTiltAction action)
		{
			if (NamedComponent == null)
				return;

			switch (action)
			{
				case eCameraTiltAction.Up:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_TILT_UP, "1");
					break;
				case eCameraTiltAction.Down:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_TILT_DOWN, "1");
					break;
				case eCameraTiltAction.Stop:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_TILT_UP, "0");
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_TILT_DOWN, "0");
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Starts zooming the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void Zoom(eCameraZoomAction action)
		{
			if (NamedComponent == null)
				return;

			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_IN, "1");
					break;
				case eCameraZoomAction.ZoomOut:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_OUT, "1");
					break;
				case eCameraZoomAction.Stop:
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_IN, "0");
					NamedComponent.SetValue(CameraNamedComponent.CONTROL_ZOOM_OUT, "0");
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

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

		/// <summary>
		/// Sets if the camera mute state should be active
		/// </summary>
		/// <param name="enable"></param>
		public void MuteCamera(bool enable)
		{
			if (NamedComponent == null)
				return;

			NamedComponent.SetValue(CameraNamedComponent.CONTROL_TOGGLE_PRIVACY, enable ? "1" : "0");
		}

		/// <summary>
		/// Resets camera to its predefined home position
		/// </summary>
		public void ActivateHome()
		{
			if (NamedComponent == null)
				return;

			NamedComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_HOME_LOAD);
		}

		/// <summary>
		/// Stores the current position as the home position.
		/// </summary>
		public void StoreHome()
		{
			if (NamedComponent == null)
				return;

			NamedComponent.Trigger(CameraNamedComponent.CONTROL_PRESET_HOME_SAVE_TRIGGER);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SnapshotComponentName = null;
			SnapshotsComponent = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(QSysCoreCameraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SnapshotComponentName = settings.SnapshotsName;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(QSysCoreCameraDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SnapshotsName = SnapshotComponentName;
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(QSysCoreCameraDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new GenericCameraRouteSourceControl<QSysCoreCameraDevice>(this, 0));
			addControl(new CameraDeviceControl(this, 1));
		}

		#endregion

		#region Camera Component Callbacks

		protected override void Subscribe(CameraNamedComponent cameraComponent)
		{
			if (cameraComponent == null)
				return;

			cameraComponent.OnControlValueUpdated += CameraComponentOnControlValueUpdated;
		}

		protected override void Unsubscribe(CameraNamedComponent cameraComponent)
		{
			if (cameraComponent == null)
				return;

			cameraComponent.OnControlValueUpdated -= CameraComponentOnControlValueUpdated;
		}

		private void CameraComponentOnControlValueUpdated(object sender, ControlValueUpdateEventArgs eventArgs)
		{
			INamedComponentControl control = sender as INamedComponentControl;
			if (control == null)
				return;

			switch (control.Name)
			{
				case CameraNamedComponent.CONTROL_TOGGLE_PRIVACY:
					ParseTogglePrivacy(eventArgs);
					break;
			}
		}

		private void ParseTogglePrivacy(ControlValueUpdateEventArgs eventArgs)
		{
			IsCameraMuted = Math.Abs(eventArgs.ValuePosition) > TOLERANCE;
		}

		#endregion
	}
}