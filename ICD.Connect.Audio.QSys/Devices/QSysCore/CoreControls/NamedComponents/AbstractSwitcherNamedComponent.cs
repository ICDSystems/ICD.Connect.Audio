using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.EventArgs;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public abstract class AbstractSwitcherNamedComponent : AbstractNamedComponent, ISwitcherNamedComponent
	{

		private const string INPUTS_COUNT_PROPERTY = "n_inputs";
		private const string OUTPUTS_COUNT_PROPERTY = "n_outputs";

		private const string SELECT_CONTROL_PREFIX = "select";

		/// <summary>
		/// Raised when an output selector value changes
		/// </summary>
		public event EventHandler<SwitcherOutputSelectChangedEventArgs> OnOutputSelectChanged;

		private int m_Outputs;

		/// <summary>
		/// Number of inputs tis switcher supports, as reported by QSys
		/// Will be 0 before the control properties are retrieved
		/// </summary>
		public int Inputs { get; private set; }

		/// <summary>
		/// Number of outputs this switcher supports, as reported by QSys
		/// Will be 0 before the control properties are retrieved
		/// </summary>
		public int Outputs
		{
			get { return m_Outputs; }
			private set
			{
				if (m_Outputs == value)
					return;

				m_Outputs = value;

				UpdateOutputControls();
			}
		}

		/// <summary>
		/// Clears and adds output select controls based on GetControlNames()
		/// </summary>
		private void UpdateOutputControls()
		{
			ClearControls();

			AddControls(GetControlNames());

			SetupInitialChangeGroups(QSysCore.Components.LoadContext, new List<int>());
		}

		/// <summary>
		/// Get names for the controls for this component
		/// Returns based on Outputs property
		/// </summary>
		/// <returns></returns>
		[NotNull]
		protected virtual IEnumerable<string> GetControlNames()
		{
			for (int i = 1; i <= Outputs; i++)
			{
				yield return string.Format("{0}.{1}", SELECT_CONTROL_PREFIX, i);
			}
		}

		protected AbstractSwitcherNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml) :
			base(context.QSysCore, friendlyName, id)
		{
			string componentName = XmlUtils.TryReadChildElementContentAsString(xml, "ComponentName");

			// If we don't have a component name, bail out
			if (string.IsNullOrEmpty(componentName))
				throw new
					InvalidOperationException(string.Format("Tried to create AbstractSwitcherNamedComponent {0}:{1} without component name",
															id, friendlyName));

			ComponentName = componentName;

			RegisterPropertyCallbacks();
		}

		protected AbstractSwitcherNamedComponent(int id, CoreElementsLoadContext context, string componentName)
			: base(context.QSysCore, string.Format("Implicit:{0}", componentName), id)
		{
			ComponentName = componentName;

			RegisterPropertyCallbacks();
		}

		/// <summary>
		/// Registers callbacks for component properties with the component
		/// If the core is initialized already, gets named components to update properties
		/// </summary>
		private void RegisterPropertyCallbacks()
		{
			AddPropertyResponseCallback(INPUTS_COUNT_PROPERTY, HandleInputsCountProperty);
			AddPropertyResponseCallback(OUTPUTS_COUNT_PROPERTY, HandleOutputsCountProperty);

			if (QSysCore.Initialized)
				QSysCore.GetNamedComponents();
		}

		/// <summary>
		/// Callback for input property updates
		/// </summary>
		/// <param name="value"></param>
		private void HandleInputsCountProperty(string value)
		{
			if (value == null)
				return;

			try
			{
				Inputs = int.Parse(value);
			}
			catch (FormatException)
			{
				QSysCore.Logger.Log(eSeverity.Error,
									"SwitcherNamedComponent-{0}:Could not parse input property value of {1} into integer",
									ComponentName, value);
			}
		}

		/// <summary>
		/// Callback for output property updates
		/// </summary>
		/// <param name="value"></param>
		private void HandleOutputsCountProperty(string value)
		{
			if (value == null)
				return;

			try
			{
				Outputs = int.Parse(value);
			}
			catch (FormatException)
			{
				QSysCore.Logger.Log(eSeverity.Error,
									"SwitcherNamedComponent-{0}:Could not parse output property value of {1} into integer",
									ComponentName, value);
			}
		}

		/// <summary>
		/// Gets the controls to subscribe to by default
		/// </summary>
		/// <returns>Controls to subscribe to</returns>
		protected override IEnumerable<INamedComponentControl> GetControlsForSubscribe()
		{
			//Subscribe to all controls
			return GetControls();
		}

		/// <summary>
		/// Called when the control value changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected sealed override void ControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			base.ControlOnValueUpdated(sender, args);

			HandleControlValueUpdate(args);
		}

		/// <summary>
		/// Called when a control value changes
		/// </summary>
		/// <param name="args"></param>
		protected virtual void HandleControlValueUpdate([NotNull] ControlValueUpdateEventArgs args)
		{
			if (args == null)
				throw new ArgumentNullException("args");

			var split = args.ControlName.Split('.');
			if (split.Length != 2)
				return;

			switch (split[0])
			{
				case SELECT_CONTROL_PREFIX:
					SelectControlValueUpdated(split[1], args.ValueRaw);
					break;
			}
		}

		/// <summary>
		/// Called when the select values update is received from QSys
		/// </summary>
		/// <param name="outputString"></param>
		/// <param name="value"></param>
		private void SelectControlValueUpdated([NotNull] string outputString, float value)
		{
			if (outputString == null)
				throw new ArgumentNullException("outputString");

			int output = int.Parse(outputString);

			int input = (int)value;

			OnOutputSelectChanged.Raise(this, new SwitcherOutputSelectChangedEventArgs(output, input));
		}

		/// <summary>
		/// Tries to get the state of the output selector
		/// Returns null if the output select control is not instantiated yet
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public int? TryGetOutputSelectState(int output)
		{
			INamedComponentControl control;
			if (!TryGetOutputSelectControl(output, out control))
				return null;

			return (int?)control.ValueRaw;

		}

		/// <summary>
		/// Tries to get the output selector control
		/// Returns false if the control is not instantiated yet
		/// </summary>
		/// <param name="output">Output number to get the selector for</param>
		/// <param name="control">Output Selector Control</param>
		/// <returns>true if a control was gotten, false if not</returns>
		protected bool TryGetOutputSelectControl(int output, out INamedComponentControl control)
		{
			control = null;
			if (output < 1 || output > Outputs)
				return false;

			string controlName = string.Format("{0}.{1}", SELECT_CONTROL_PREFIX, output);

			return TryGetControl(controlName, out control);
		}

		/// <summary>
		/// Tries to set the output selector for the given output to the given value
		/// Returns false if the control is not instantiated yet
		/// </summary>
		/// <param name="output"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		public virtual  bool TrySetOutput(int output, int? input)
		{
			INamedComponentControl outputSelect;
			if (!TryGetOutputSelectControl(output, out outputSelect))
				return false;

			if (input.HasValue)
				outputSelect.SetValue(string.Format("{0}", input.Value));

			return true;
		}
	}
}
