using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Microphone;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.Shure.Devices;

namespace ICD.Connect.Audio.Shure.Controls
{
	public sealed class ShureMicrophoneDeviceControl : AbstractMicrophoneDeviceControl<IShureMicDevice>
	{
		#region Properties

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 1400; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ShureMicrophoneDeviceControl(IShureMicDevice parent, int id)
			: base(parent, id)
		{
			SupportedVolumeFeatures = eVolumeFeatures.Mute |
									  eVolumeFeatures.MuteAssignment |
									  eVolumeFeatures.MuteFeedback |
									  eVolumeFeatures.Volume |
									  eVolumeFeatures.VolumeAssignment |
									  eVolumeFeatures.VolumeFeedback;

			IsMuted = Parent.IsMuted;
			VolumeLevel = Parent.AudioGain;
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			Parent.SetIsMuted(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			Parent.SetIsMuted(!Parent.IsMuted);
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			Parent.SetAudioGain(level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			Parent.SetAudioGain(Parent.AudioGain + 10);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			Parent.SetAudioGain(Parent.AudioGain - 10);
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

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="level"></param>
		public override void SetAnalogGainLevel(float level)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		public override void SetPhantomPower(bool power)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Device Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(IShureMicDevice parent)
		{
			base.Subscribe(parent);

			parent.OnAudioGainChanged += ParentOnAudioGainChanged;
			parent.OnIsMutedChanged += ParentOnIsMutedChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IShureMicDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnAudioGainChanged -= ParentOnAudioGainChanged;
			parent.OnIsMutedChanged -= ParentOnIsMutedChanged;
		}

		/// <summary>
		/// Called when the parent mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ParentOnIsMutedChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IsMuted = Parent.IsMuted;
		}

		/// <summary>
		/// Called when the parent audio gain changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="intEventArgs"></param>
		private void ParentOnAudioGainChanged(object sender, IntEventArgs intEventArgs)
		{
			VolumeLevel = Parent.AudioGain;
		}

		#endregion
	}
}
