using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.Router
{
	public sealed class RouterOutput : AbstractAttributeChild<RouterBlock>
	{
		private const string INPUT_ATTRIBUTE = "input";
		private const string LABEL_ATTRIBUTE = "outputLabel";

		public event EventHandler<IntEventArgs> OnInputChanged;
		public event EventHandler<StringEventArgs> OnLabelChanged;

		private string m_Label;
		private int m_Input;

		#region Properties

		[PublicAPI]
		public int Input
		{
			get { return m_Input; }
			private set
			{
				if (value == m_Input)
					return;

				m_Input = value;

				Log(eSeverity.Informational, "Input set to {0}", m_Input);

				OnInputChanged.Raise(this, new IntEventArgs(m_Input));
			}
		}

		[PublicAPI]
		public string Label
		{
			get { return m_Label; }
			private set
			{
				if (value == m_Label)
					return;

				m_Label = value;

				Log(eSeverity.Informational, "Label set to {0}", m_Label);

				OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public RouterOutput(RouterBlock parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnLabelChanged = null;
			OnInputChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(InputFeedback, AttributeCode.eCommand.Get, INPUT_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the label of the output.
		/// </summary>
		/// <param name="label"></param>
		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		/// <summary>
		/// Clears the input for this output.
		/// </summary>
		[PublicAPI]
		public void ClearInput()
		{
			SetInput(0);
		}

		[PublicAPI]
		public void SetInput(int input)
		{
			RequestAttribute(InputFeedback, AttributeCode.eCommand.Set, INPUT_ATTRIBUTE, new Value(input), Index);
		}

		[PublicAPI]
		public void IncrementInput()
		{
			RequestAttribute(InputFeedback, AttributeCode.eCommand.Increment, INPUT_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementInput()
		{
			RequestAttribute(InputFeedback, AttributeCode.eCommand.Decrement, INPUT_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Label = innerValue.StringValue;
		}

		private void InputFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Input = innerValue.IntValue;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Label", Label);
			addRow("Input", Input);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("SetLabel", "SetLabel <LABEL>", s => SetLabel(s));
			yield return new ConsoleCommand("ClearInput", "", () => ClearInput());
			yield return new GenericConsoleCommand<int>("SetInput", "SetInput <INPUT>", i => SetInput(i));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
