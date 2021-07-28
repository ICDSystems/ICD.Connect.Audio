using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls.State
{
	/// <summary>
	/// Mutes/unmutes a room combiner room by setting the source.
	/// </summary>
	public sealed class RoomCombinerRoomStateControl : AbstractBiampTesiraStateDeviceControl, IVolumeDeviceControl
	{
		private readonly int m_MuteSource;
		private readonly int m_UnmuteSource;

		[NotNull]
		private readonly RoomCombinerRoom m_Room;

		[NotNull]
		private readonly IBiampTesiraStateDeviceControl m_Feedback;

		#region Events

		public event EventHandler<VolumeControlIsMutedChangedApiEventArgs> OnIsMutedChanged;
		public event EventHandler<VolumeControlVolumeChangedApiEventArgs> OnVolumeChanged;
		public event EventHandler<VolumeControlSupportedVolumeFeaturesChangedApiEventArgs> OnSupportedVolumeFeaturesChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the state of the control.
		/// </summary>
		public override bool State { get { return base.State; }
			protected set
			{
				if (value == State)
					return;

				base.State = value;

				OnIsMutedChanged.Raise(this, new VolumeControlIsMutedChangedApiEventArgs(value));
			} }

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures
		{
			get { return eVolumeFeatures.Mute | eVolumeFeatures.MuteAssignment | eVolumeFeatures.MuteFeedback; }
		}

		/// <summary>
		/// Gets the muted state.
		/// Will return false if mute feedback is not supported.
		/// </summary>
		public bool IsMuted { get { return State; } }

		/// <summary>
		/// Gets the current volume in the range VolumeLevelMin to VolumeLevelMax.
		/// </summary>
		public float VolumeLevel { get { return 0; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public float VolumeLevelMax { get { return 0; } }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		public string VolumeString { get { return string.Empty; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="muteSource"></param>
		/// <param name="unmuteSource"></param>
		/// <param name="room"></param>
		/// <param name="feedback"></param>
		public RoomCombinerRoomStateControl(int id, Guid uuid, string name, int muteSource, int unmuteSource,
		                                    [NotNull] RoomCombinerRoom room,
		                                    [NotNull] IBiampTesiraStateDeviceControl feedback)
			: base(id, uuid, name, room.Device)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			if (feedback == null)
				throw new ArgumentNullException("feedback");

			m_MuteSource = muteSource;
			m_UnmuteSource = unmuteSource;
			m_Room = room;
			m_Feedback = feedback;

			Subscribe(m_Feedback);
			State = m_Feedback.State;
		}

		#region Methods

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state"></param>
		public override void SetState(bool state)
		{
			m_Room.SetSourceSelection(state ? m_MuteSource : m_UnmuteSource);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetIsMuted(bool mute)
		{
			SetState(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void ToggleIsMuted()
		{
			SetState(!State);
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public void SetVolumeLevel(float level)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeIncrement()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeDecrement()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Feedback);
		}

		#endregion

		#region Feedback Callbacks

		/// <summary>
		/// Subscribe to the state control events.
		/// </summary>
		/// <param name="feedback"></param>
		private void Subscribe(IBiampTesiraStateDeviceControl feedback)
		{
			feedback.OnStateChanged += FeedbackOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the state control events.
		/// </summary>
		/// <param name="feedback"></param>
		private void Unsubscribe(IBiampTesiraStateDeviceControl feedback)
		{
			feedback.OnStateChanged -= FeedbackOnStateChanged;
		}

		/// <summary>
		/// Called when the state control state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void FeedbackOnStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			State = boolEventArgs.Data;
		}

		#endregion
	}
}
