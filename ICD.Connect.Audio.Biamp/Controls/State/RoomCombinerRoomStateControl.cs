using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	/// <summary>
	/// Mutes/unmutes a room combiner room by setting the source.
	/// </summary>
	public sealed class RoomCombinerRoomStateControl : AbstractBiampTesiraStateDeviceControl
	{
		private readonly int m_MuteSource;
		private readonly int m_UnmuteSource;

		[NotNull]
		private readonly RoomCombinerRoom m_Room;

		[NotNull]
		private readonly IBiampTesiraStateDeviceControl m_Feedback;

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

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Feedback);
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state"></param>
		public override void SetState(bool state)
		{
			m_Room.SetSourceSelection(state ? m_MuteSource : m_UnmuteSource);
		}

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
