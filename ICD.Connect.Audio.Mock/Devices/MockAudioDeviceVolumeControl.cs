using System;
using ICD.Common.Utils;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Mock.Devices
{
	public sealed class MockAudioDeviceVolumeControl : AbstractVolumeDeviceControl<IDeviceBase>
	{
		#region Properties

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public override eVolumeFeatures SupportedVolumeFeatures
		{
			get
			{
				return eVolumeFeatures.Mute |
				       eVolumeFeatures.MuteAssignment |
				       eVolumeFeatures.MuteFeedback |
				       eVolumeFeatures.Volume |
				       eVolumeFeatures.VolumeAssignment |
				       eVolumeFeatures.VolumeFeedback;
			}
		}

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public override float VolumeLevelMax { get { return 100; } }

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

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			IsMuted = mute;
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			IsMuted = !IsMuted;
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			VolumeLevel = MathUtils.Clamp(level, VolumeLevelMin, VolumeLevelMax);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			SetVolumeLevel(VolumeLevel + 1);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			SetVolumeLevel(VolumeLevel - 1);
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}
