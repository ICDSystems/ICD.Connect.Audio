using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Biamp.Controls.Volume
{
	public sealed class BiampTesiraPrivacyMuteDeviceControl : AbstractVolumeDeviceControl<BiampTesiraDevice>
	{
		private readonly string m_Name;
		private readonly IStateAttributeInterface m_StateInterface;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 0; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="stateInterface"></param>
		public BiampTesiraPrivacyMuteDeviceControl(int id, Guid uuid, string name,
		                                           IStateAttributeInterface stateInterface)
			: base(stateInterface.Device, id, uuid)
		{
			m_Name = name;
			m_StateInterface = stateInterface;

			SupportedVolumeFeatures = eVolumeFeatures.Mute |
			                          eVolumeFeatures.MuteAssignment |
			                          eVolumeFeatures.MuteFeedback;

			Subscribe(m_StateInterface);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_StateInterface);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			m_StateInterface.SetState(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			m_StateInterface.SetState(!m_StateInterface.State);
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			throw new NotSupportedException();
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

		#region Volume Interface Callbacks

		/// <summary>
		/// Subscribe to the state interface events.
		/// </summary>
		/// <param name="stateInterface"></param>
		private void Subscribe(IStateAttributeInterface stateInterface)
		{
			stateInterface.OnStateChanged += StateInterfaceOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the state interface events.
		/// </summary>
		/// <param name="stateInterface"></param>
		private void Unsubscribe(IStateAttributeInterface stateInterface)
		{
			stateInterface.OnStateChanged -= StateInterfaceOnStateChanged;
		}

		/// <summary>
		/// Called when the state interface state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void StateInterfaceOnStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IsMuted = m_StateInterface.State;
		}

		#endregion
	}
}