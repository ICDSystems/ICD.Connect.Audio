using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	/// <summary>
	/// Mutes/unmutes a room combiner source by setting the source label.
	/// </summary>
	public sealed class RoomCombinerSourceStateControl : AbstractBiampTesiraStateDeviceControl
	{
		private readonly string m_MuteLabel;
		private readonly string m_UnmuteLabel;
		private readonly RoomCombinerSource m_Source;
		private readonly IBiampTesiraStateDeviceControl m_Feedback;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="unmuteLabel"></param>
		/// <param name="source"></param>
		/// <param name="feedback"></param>
		/// <param name="muteLabel"></param>
		public RoomCombinerSourceStateControl(int id, string name, string muteLabel, string unmuteLabel,
											  RoomCombinerSource source, IBiampTesiraStateDeviceControl feedback)
			: base(id, name, source.Device)
		{
			m_MuteLabel = muteLabel;
			m_UnmuteLabel = unmuteLabel;
			m_Source = source;

			m_Source = source;
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
			m_Source.SetLabel(state ? m_MuteLabel : m_UnmuteLabel);
		}

		#region Feedback Callbacks

		private void Subscribe(IBiampTesiraStateDeviceControl feedback)
		{
			feedback.OnStateChanged += FeedbackOnStateChanged;
		}

		private void Unsubscribe(IBiampTesiraStateDeviceControl feedback)
		{
			feedback.OnStateChanged -= FeedbackOnStateChanged;
		}

		private void FeedbackOnStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			State = boolEventArgs.Data;
		}

		#endregion
	}
}
