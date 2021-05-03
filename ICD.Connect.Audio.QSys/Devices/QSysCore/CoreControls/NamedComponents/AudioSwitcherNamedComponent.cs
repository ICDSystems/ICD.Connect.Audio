using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.EventArgs;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public sealed class AudioSwitcherNamedComponent : AbstractSwitcherNamedComponent
	{
		private const string MUTE_CONTROL_PREFIX = "mute";

		/// <summary>
		/// Raised when an output mute state changes
		/// </summary>
		public event EventHandler<SwitcherOutputMuteChangedEventArgs> OnOutputMuteChanged;

		/// <summary>
		/// Get names for the controls for this component
		/// Returns based on Outputs property
		/// </summary>
		/// <returns></returns>
		protected override IEnumerable<string> GetControlNames()
		{
			foreach (string name in GetBaseControlNames())
				yield return name;

			for (int i = 1; i <= Outputs; i++)
				yield return string.Format("{0}.{1}", MUTE_CONTROL_PREFIX, i);
		}

		private IEnumerable<string> GetBaseControlNames()
		{
			return base.GetControlNames();
		}

		[UsedImplicitly]
		public AudioSwitcherNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml) :
			base(id, friendlyName, context, xml)
		{
		}

		[UsedImplicitly]
		public AudioSwitcherNamedComponent(int id, CoreElementsLoadContext context, string componentName)
			: base(id, context, componentName)
		{
		}

		/// <summary>
		/// Called when a control value changes
		/// </summary>
		/// <param name="args"></param>
		protected override void HandleControlValueUpdate(ControlValueUpdateEventArgs args)
		{
			if (args == null)
				throw new ArgumentNullException("args");

			string[] split = args.ControlName.Split('.');
			if (split.Length != 2)
			{
				base.HandleControlValueUpdate(args);
				return;
			}

			switch (split[0])
			{
				case MUTE_CONTROL_PREFIX:
					MuteControlValueUpdated(split[1], args.ValueRaw);
					break;
				default:
					base.HandleControlValueUpdate(args);
					break;
			}

			
		}

		/// <summary>
		/// Called when the mute state update is received from QSys
		/// </summary>
		/// <param name="outputString"></param>
		/// <param name="value"></param>
		private void MuteControlValueUpdated(string outputString, float value)
		{
			int output = int.Parse(outputString);

			bool muted = Math.Abs(value) > 0;

			OnOutputMuteChanged.Raise(this, new SwitcherOutputMuteChangedEventArgs(output, muted));

		}

		/// <summary>
		/// Tries to get the state of the output mute
		/// Returns null if the output mute control is not instantiated yet
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public bool? TryGetOutputMuteState(int output)
		{
			INamedComponentControl control;
			if (!TryGetMuteControl(output, out control))
				return null;

			return Math.Abs(control.ValueRaw) > 0;
		}

		/// <summary>
		/// Tries to get the output mute control
		/// Returns false if the control is not instantiated yet
		/// </summary>
		/// <param name="output">Output number to get the mute for</param>
		/// <param name="control">Output Mute Control</param>
		/// <returns>true if a control was gotten, false if not</returns>
		private bool TryGetMuteControl(int output, out INamedComponentControl control)
		{
			control = null;

			if (output < 1 || output > Outputs)
				return false;

			string controlName = string.Format("{0}.{1}", MUTE_CONTROL_PREFIX, output);

			return TryGetControl(controlName, out control);
		}

		/// <summary>
		/// Tries to set the output selector for the given output to the given value
		/// Returns false if the control is not instantiated yet
		/// </summary>
		/// <param name="output"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool TrySetOutput(int output, int? input)
		{
			if (!base.TrySetOutput(output, input))
				return false;

			INamedComponentControl outputMute;
			if (!TryGetMuteControl(output, out outputMute))
				return false;

			// Set mute to true to clear output, false to allow
			outputMute.SetValue(!input.HasValue);

			return true;
		}
	}
}
