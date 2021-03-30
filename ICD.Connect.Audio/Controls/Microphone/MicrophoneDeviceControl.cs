using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Devices.Microphones;

namespace ICD.Connect.Audio.Controls.Microphone
{
	public sealed class MicrophoneDeviceControl : AbstractMicrophoneDeviceControl<IMicrophoneDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public MicrophoneDeviceControl(IMicrophoneDevice parent, int id)
			: base(parent, id)
		{
			IsMuted = Parent.IsMuted;
			PhantomPower = Parent.PhantomPower;
			GainLevel = Parent.GainLevel;
		}

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetGainLevel(float volume)
		{
			Parent.SetGainLevel(volume);
		}

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetMuted(bool mute)
		{
			Parent.SetMuted(mute);
		}

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		public override void SetPhantomPower(bool power)
		{
			Parent.SetPhantomPower(power);
		}

		#endregion

		#region Device Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(IMicrophoneDevice parent)
		{
			base.Subscribe(parent);

			parent.OnMuteStateChanged += ParentOnMuteStateChanged;
			parent.OnPhantomPowerStateChanged += ParentOnPhantomPowerStateChanged;
			parent.OnGainLevelChanged += ParentOnGainLevelChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IMicrophoneDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnMuteStateChanged -= ParentOnMuteStateChanged;
			parent.OnPhantomPowerStateChanged -= ParentOnPhantomPowerStateChanged;
			parent.OnGainLevelChanged -= ParentOnGainLevelChanged;
		}

		/// <summary>
		/// Called when the parent mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ParentOnMuteStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IsMuted = Parent.IsMuted;
		}

		/// <summary>
		/// Called when the parent phantom power state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ParentOnPhantomPowerStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			PhantomPower = Parent.PhantomPower;
		}

		/// <summary>
		/// Called when the parent gain level changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="floatEventArgs"></param>
		private void ParentOnGainLevelChanged(object sender, FloatEventArgs floatEventArgs)
		{
			GainLevel = Parent.GainLevel;
		}

		#endregion
	}
}
