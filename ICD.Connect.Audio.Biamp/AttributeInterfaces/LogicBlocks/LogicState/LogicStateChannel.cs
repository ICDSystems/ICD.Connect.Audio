using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks.LogicState
{
	public sealed class LogicStateChannel : AbstractAttributeChild<LogicStateBlock>, IStateAttributeInterface
	{
		private const string LABEL_ATTRIBUTE = "label";
		private const string STATE_ATTRIBUTE = "state";

		public event EventHandler<StringEventArgs> OnLabelChanged;
		public event EventHandler<BoolEventArgs> OnStateChanged; 

		private string m_Label;
		private bool m_State;

		#region Properties

		[PublicAPI]
		public string Label
		{
			get { return m_Label; }
			private set
			{
				if (value == m_Label)
					return;

				m_Label = value;

				OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
			}
		}

		[PublicAPI]
		public bool State
		{
			get { return m_State; }
			private set
			{
				if (value == m_State)
					return;

				m_State = value;

				OnStateChanged.Raise(this, new BoolEventArgs(m_State));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public LogicStateChannel(LogicStateBlock parent, int index)
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
			OnStateChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial value
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(StateFeedback, AttributeCode.eCommand.Get, STATE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		[PublicAPI]
		public void SetState(bool state)
		{
			RequestAttribute(StateFeedback, AttributeCode.eCommand.Set, STATE_ATTRIBUTE, new Value(state), Index);
		}

		[PublicAPI]
		public void ToggleState()
		{
			RequestAttribute(StateFeedback, AttributeCode.eCommand.Toggle, STATE_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Feedback

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Label = (value["value"] as Value).StringValue;
		}

		private void StateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			State = (value["value"] as Value).BoolValue;
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
			addRow("State", State);
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

			yield return new GenericConsoleCommand<bool>("SetState", "SetState <true/false>", b => SetState(b));
			yield return new ConsoleCommand("ToggleState", "", () => ToggleState());
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
