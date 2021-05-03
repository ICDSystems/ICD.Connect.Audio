using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.Switchers.AudioSwitcher;
using ICD.Connect.Audio.QSys.EventArgs;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.Controls
{
	class AudioSwitcherRouteSwitchControl : AbstractSwitcherRouteSwitchControl<AudioSwitcherQSysDevice, AudioSwitcherNamedComponent>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public AudioSwitcherRouteSwitchControl(AudioSwitcherQSysDevice parent, int id) : base(parent, id, eConnectionType.Audio)
		{
		}

		/// <summary>
		/// Update the current state for the given output
		/// </summary>
		/// <param name="output"></param>
		protected override void UpdateCurrentState(int output)
		{
			if (SwitcherComponent == null)
				return;

			bool? mute = SwitcherComponent.TryGetOutputMuteState(output);
			if (!mute.HasValue)
				return;

			if (mute.Value)
				SwitcherCache.SetInputForOutput(output, null, ConnectionMask);
			else
				base.UpdateCurrentState(output);
		}

		/// <summary>
		/// Subscribe to the switcher component
		/// </summary>
		/// <remarks>Called from AbstractRouteSwitchControl constructor</remarks>
		/// <param name="switcherComponent"></param>
		protected override void Subscribe(AudioSwitcherNamedComponent switcherComponent)
		{
			base.Subscribe(switcherComponent);

			if (switcherComponent == null)
				return;

			switcherComponent.OnOutputMuteChanged += SwitcherComponentOnOnOutputMuteChanged;
		}

		/// <summary>
		/// Unsubscribe from the switcher component
		/// </summary>
		/// <param name="switcherComponent"></param>
		protected override void Unsubscribe(AudioSwitcherNamedComponent switcherComponent)
		{
			base.Unsubscribe(switcherComponent);

			if (switcherComponent == null)
				return;

			switcherComponent.OnOutputMuteChanged -= SwitcherComponentOnOnOutputMuteChanged;
		}

		private void SwitcherComponentOnOnOutputMuteChanged(object sender, SwitcherOutputMuteChangedEventArgs args)
		{
			SwitcherCache.SetInputForOutput(args.Output,
			                                args.MuteState ? null : SwitcherComponent.TryGetOutputSelectState(args.Output),
			                                ConnectionMask);
		}
	}
}
