using ICD.Connect.Audio.Biamp.AttributeInterfaces;
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
		                                      RoomCombinerSource source, IStateAttributeInterface feedback)
			: base(id, name, feedback)
		{
			m_MuteLabel = muteLabel;
			m_UnmuteLabel = unmuteLabel;
			m_Source = source;
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state"></param>
		public override void SetState(bool state)
		{
			m_Source.SetLabel(state ? m_MuteLabel : m_UnmuteLabel);
		}
	}
}
