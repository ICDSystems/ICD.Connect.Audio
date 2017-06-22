using System;
using ICD.Common.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	/// <summary>
	/// Wraps a logic block to provide a simple on/off switch.
	/// </summary>
	public sealed class BiampTesiraStateDeviceControl : AbstractDeviceControl<BiampTesiraDevice>
	{
		/// <summary>
		/// Raised when the state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnStateChanged;

		private readonly string m_Name;
		private readonly IStateAttributeInterface m_StateAttribute;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the state of the control.
		/// </summary>
		public bool State { get { return m_StateAttribute.State; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="stateAttribute"></param>
		public BiampTesiraStateDeviceControl(int id, string name, IStateAttributeInterface stateAttribute)
			: base(stateAttribute.Device, id)
		{
			m_Name = name;
			m_StateAttribute = stateAttribute;

			Subscribe(m_StateAttribute);
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
		public void SetState(bool state)
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
			OnStateChanged.Raise(this, new BoolEventArgs(args.Data));
		}

		#endregion
	}
}
