using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Mock.Devices
{
	public sealed class MockAudioDeviceVolumeControl : AbstractDeviceControl<IDeviceBase>, IVolumeMuteFeedbackDeviceControl,
	                                                   IVolumeRawLevelDeviceControl
	{
		private bool m_VolumeIsMuted;
		private float m_VolumeRaw;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted
		{
			get { return m_VolumeIsMuted; }
			set
			{
				if (value == m_VolumeIsMuted)
					return;
				
				m_VolumeIsMuted = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - VolumeIsMuted changed to {1}", this, m_VolumeIsMuted);

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_VolumeIsMuted));
			}
		}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public float VolumeRaw
		{
			get { return m_VolumeRaw; }
			set
			{
				value = MathUtils.Clamp(value, VolumeRawMinRange, VolumeRawMaxRange);

				if (Math.Abs(value - m_VolumeRaw) < 0.01f)
					return;

				m_VolumeRaw = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - VolumeRaw changed to {1}", this, m_VolumeRaw);

				OnVolumeChanged.Raise(this, new VolumeDeviceVolumeChangedEventArgs(m_VolumeRaw, VolumePosition, VolumeString));
			}
		}

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public float VolumePosition { get { return MathUtils.MapRange(VolumeRawMinRange, VolumeRawMaxRange, 0, 1, VolumeRaw); } }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public string VolumeString { get { return VolumeRaw.ToString(); } }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public float? VolumeRawMax { get { return null; } }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		public float? VolumeRawMin { get { return null; } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeRawMaxRange { get { return 100; } }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeRawMinRange { get { return 0; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public MockAudioDeviceVolumeControl(IDeviceBase parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnMuteStateChanged = null;
			OnVolumeChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			VolumeIsMuted = !VolumeIsMuted;
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			VolumeIsMuted = mute;
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeLevelIncrement()
		{
			VolumeRaw++;
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeLevelDecrement()
		{
			VolumeRaw--;
		}

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampUp()
		{
			// TODO
			VolumeLevelIncrement();
		}

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeLevelRampStop"/> must be called after
		/// </summary>
		public void VolumeLevelRampDown()
		{
			// TODO
			VolumeLevelDecrement();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeLevelRampStop()
		{
			// TODO
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public void SetVolumeRaw(float volume)
		{
			VolumeRaw = volume;
		}

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public void SetVolumePosition(float position)
		{
			VolumeRaw = MathUtils.MapRange(0, 1, VolumeRawMinRange, VolumeRawMaxRange, position);
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeLevelIncrement(float incrementValue)
		{
			VolumeRaw += incrementValue;
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeLevelDecrement(float decrementValue)
		{
			VolumeRaw -= decrementValue;
		}

		#endregion
	}
}
