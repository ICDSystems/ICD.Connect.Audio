using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	/// <summary>
	/// Wraps a logic block to provide a simple on/off switch.
	/// </summary>
	public sealed class BiampTesiraStateDeviceControl : AbstractBiampTesiraStateDeviceControl
	{
		private readonly IStateAttributeInterface m_StateAttribute;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="stateAttribute"></param>
		public BiampTesiraStateDeviceControl(int id, Guid uuid, string name, IStateAttributeInterface stateAttribute)
			: base(id, uuid, name, stateAttribute.Device)
		{
			m_StateAttribute = stateAttribute;

			Subscribe(m_StateAttribute);
			State = m_StateAttribute.State;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_StateAttribute);
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state"></param>
		public override void SetState(bool state)
		{
			m_StateAttribute.SetState(state);
		}

		#region Channel Callbacks

		private void Subscribe(IStateAttributeInterface stateChannel)
		{
			stateChannel.OnStateChanged += StateChannelOnStateChanged;
		}

		private void Unsubscribe(IStateAttributeInterface stateChannel)
		{
			stateChannel.OnStateChanged -= StateChannelOnStateChanged;
		}

		private void StateChannelOnStateChanged(object sender, BoolEventArgs args)
		{
			State = m_StateAttribute.State;
		}

		#endregion
	}
}
