using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
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
		public IDeviceBase ActiveDevice { get { return m_ActiveDevice; } set { m_ActiveDevice = value; } }

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public float VolumeRaw { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public float VolumePosition { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public string VolumeString { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public float? VolumeRawMax { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		public float? VolumeRawMin { get { throw new NotImplementedException(); } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeRawMaxRange { get { throw new NotImplementedException(); } }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeRawMinRange { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted { get { throw new NotImplementedException(); } }

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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeLevelDecrement()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampUp()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampDown()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeLevelRampStop()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public void SetVolumeRaw(float volume)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public void SetVolumePosition(float position)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeLevelIncrement(float incrementValue)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeLevelDecrement(float decrementValue)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			throw new NotImplementedException();
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
