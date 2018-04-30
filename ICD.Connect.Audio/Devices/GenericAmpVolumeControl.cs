using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Routing.EventArguments;

namespace ICD.Connect.Audio.Devices
{
	public sealed class GenericAmpVolumeControl : AbstractDeviceControl<GenericAmpDevice>, IVolumeRawLevelDeviceControl,
	                                              IVolumeMuteFeedbackDeviceControl
	{
		#region Events

		/// <summary>
		/// Raised when the active controlled audio device changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<IDeviceBase>> OnActiveDeviceChanged;

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		private GenericAmpRouteSwitcherControl m_Switcher;
		private IDeviceBase m_ActiveDevice;

		#region Properties

		/// <summary>
		/// Gets the device that is currently being controlled for volume.
		/// </summary>
		[PublicAPI]
		[CanBeNull]
		public IDeviceBase ActiveDevice 
		{
			get
			{
				return m_ActiveDevice;
			}
			set
			{
				if (value == m_ActiveDevice)
					return;

				m_ActiveDevice = value;

				OnActiveDeviceChanged.Raise(this, new GenericEventArgs<IDeviceBase>(m_ActiveDevice));
			} 
		}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public float VolumeRaw { get { return ActiveDeviceHelper<IVolumeLevelDeviceControl, float>(c => c.VolumeRaw); } }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public float VolumePosition { get { return ActiveDeviceHelper<IVolumeLevelDeviceControl, float>(c => c.VolumePosition); } }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public string VolumeString { get { return ActiveDeviceHelper<IVolumeLevelDeviceControl, string>(c => c.VolumeString); } }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public float? VolumeRawMax { get { return ActiveDeviceHelper<IVolumeLevelDeviceControl, float?>(c => c.VolumeRawMax); } }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		public float? VolumeRawMin { get { return ActiveDeviceHelper<IVolumeLevelDeviceControl, float?>(c => c.VolumeRawMin); } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeRawMaxRange { get { return ActiveDeviceHelper<IVolumeRawLevelDeviceControl, float>(c => c.VolumeRawMaxRange); } }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeRawMinRange { get { return ActiveDeviceHelper<IVolumeRawLevelDeviceControl, float>(c => c.VolumeRawMinRange); } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted { get { return ActiveDeviceHelper<IVolumeMuteFeedbackDeviceControl, bool>(c => c.VolumeIsMuted); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public GenericAmpVolumeControl(GenericAmpDevice parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnActiveDeviceChanged = null;
			OnVolumeChanged = null;
			OnMuteStateChanged = null;

			Unsubscribe(Parent);

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeLevelIncrement()
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.VolumeLevelIncrement());
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeLevelDecrement()
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.VolumeLevelDecrement());
		}

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampUp()
		{
			ActiveDeviceHelper<IVolumeLevelBasicDeviceControl>(c => c.VolumeLevelRampUp());
		}

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampDown()
		{
			ActiveDeviceHelper<IVolumeLevelBasicDeviceControl>(c => c.VolumeLevelRampDown());
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeLevelRampStop()
		{
			ActiveDeviceHelper<IVolumeLevelBasicDeviceControl>(c => c.VolumeLevelRampStop());
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public void SetVolumeRaw(float volume)
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.SetVolumeRaw(volume));
		}

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public void SetVolumePosition(float position)
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.SetVolumePosition(position));
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeLevelIncrement(float incrementValue)
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.VolumeLevelIncrement(incrementValue));
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeLevelDecrement(float decrementValue)
		{
			ActiveDeviceHelper<IVolumeLevelDeviceControl>(c => c.VolumeLevelIncrement(decrementValue));
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			ActiveDeviceHelper<IVolumeMuteBasicDeviceControl>(c => c.VolumeMuteToggle());
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			ActiveDeviceHelper<IVolumeMuteDeviceControl>(c => c.SetVolumeMute(mute));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Helper method for performing an action for the given control type on the current active device.
		/// Returns default result if there is no active device or the control is null.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <param name="callback"></param>
		/// <returns></returns>
		private void ActiveDeviceHelper<TControl>(Action<TControl> callback)
			where TControl : IVolumeDeviceControl
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			if (m_ActiveDevice == null)
				return;

			TControl control = m_ActiveDevice.Controls.GetControl<TControl>();

// ReSharper disable once CompareNonConstrainedGenericWithNull
			if (control != null)
				callback(control);
		}

		/// <summary>
		/// Helper method for performing an action for the given control type on the current active device.
		/// Returns default result if there is no active device or the control is null.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="callback"></param>
		/// <returns></returns>
		private TResult ActiveDeviceHelper<TControl, TResult>(Func<TControl, TResult> callback)
			where TControl : IVolumeDeviceControl
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			TResult output = default(TResult);
			ActiveDeviceHelper<TControl>(c => output = callback(c));
			return output;
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Subscribe(GenericAmpDevice parent)
		{
			m_Switcher = parent.Controls.GetControl<GenericAmpRouteSwitcherControl>();
			if (m_Switcher == null)
				throw new InvalidOperationException();

			m_Switcher.OnActiveInputsChanged += SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Unsubscribe(GenericAmpDevice parent)
		{
			m_Switcher.OnActiveInputsChanged -= SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Called when the active input changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SwitcherOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs eventArgs)
		{
			IDeviceBase activeDevice = FindAudioDevice();
			SetActiveDevice(activeDevice);
		}

		/// <summary>
		/// Walks the routing graph to find the closest active audio device.
		/// </summary>
		/// <returns></returns>
		private IDeviceBase FindAudioDevice()
		{
			throw new NotImplementedException();
		}

		private void SetActiveDevice(IDeviceBase activeDevice)
		{
			if (activeDevice == ActiveDevice)
				return;


		}

		#endregion
	}
}
