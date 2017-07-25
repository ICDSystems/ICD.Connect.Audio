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

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MeterBlocks
{
	public sealed class PeakOrRmsMeterChannel : AbstractAttributeChild<PeakOrRmsMeterBlock>
	{
		private const string HOLD_ENABLED_ATTRIBUTE = "holdEnabled";
		private const string HOLD_TIME_ATTRIBUTE = "holdTime";
		private const string HOLD_INDEFINITELY_ATTRIBUTE = "indefiniteHold";
		private const string LABEL_ATTRIBUTE = "label";
		private const string LEVEL_ATTRIBUTE = "level";

		public event EventHandler<BoolEventArgs> OnHoldEnabledChanged;
		public event EventHandler<FloatEventArgs> OnHoldTimeChanged;
		public event EventHandler<BoolEventArgs> OnHoldIndefinitelyChanged;
		public event EventHandler<StringEventArgs> OnLabelChanged;
		public event EventHandler<FloatEventArgs> OnLevelChanged; 

		private bool m_HoldEnabled;
		private float m_HoldTime;
		private bool m_HoldIndefinitely;
		private string m_Label;
		private float m_Level;

		#region Properties

		[PublicAPI]
		public bool HoldEnabled
		{
			get { return m_HoldEnabled; }
			private set
			{
				if (value == m_HoldEnabled)
					return;

				m_HoldEnabled = value;

				Log(eSeverity.Informational, "HoldEnabled set to {0}", m_HoldEnabled);

				OnHoldEnabledChanged.Raise(this, new BoolEventArgs(m_HoldEnabled));
			}
		}

		[PublicAPI]
		public float HoldTime
		{
			get { return m_HoldTime; }
			private set
			{
				if (Math.Abs(value - m_HoldTime) < 0.01f)
					return;

				m_HoldTime = value;

				Log(eSeverity.Informational, "HoldTime set to {0}", m_HoldTime);

				OnHoldTimeChanged.Raise(this, new FloatEventArgs(m_HoldTime));
			}
		}

		[PublicAPI]
		public bool HoldIndefinitely
		{
			get { return m_HoldIndefinitely; }
			private set
			{
				if (value == m_HoldIndefinitely)
					return;

				m_HoldIndefinitely = value;

				Log(eSeverity.Informational, "HoldIndefinitely set to {0}", m_HoldIndefinitely);

				OnHoldIndefinitelyChanged.Raise(this, new BoolEventArgs(m_HoldIndefinitely));
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

		[PublicAPI]
		public float Level
		{
			get { return m_Level; }
			private set
			{
				if (Math.Abs(value - m_Level) < 0.01f)
					return;

				m_Level = value;

				Log(eSeverity.Informational, "Level set to {0}", m_Level);

				OnLevelChanged.Raise(this, new FloatEventArgs(m_Level));
			}
		}


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Channel"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public PeakOrRmsMeterChannel(PeakOrRmsMeterBlock parent, int index)
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
			OnHoldEnabledChanged = null;
			OnHoldTimeChanged = null;
			OnHoldIndefinitelyChanged = null;
			OnLabelChanged = null;
			OnLevelChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Unsubscribe, LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(HoldEnabledFeedback, AttributeCode.eCommand.Get, HOLD_ENABLED_ATTRIBUTE, null, Index);
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Get, HOLD_TIME_ATTRIBUTE, null, Index);
			RequestAttribute(HoldIndefinitelyFeedback, AttributeCode.eCommand.Get, HOLD_INDEFINITELY_ATTRIBUTE, null, Index);
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);

			// Subscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Subscribe, LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetHoldEnabled(bool enabled)
		{
			RequestAttribute(HoldEnabledFeedback, AttributeCode.eCommand.Set, HOLD_ENABLED_ATTRIBUTE, new Value(enabled), Index);
		}

		[PublicAPI]
		public void ToggleHoldEnabled()
		{
			RequestAttribute(HoldEnabledFeedback, AttributeCode.eCommand.Toggle, HOLD_ENABLED_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetHoldTime(float holdTime)
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Set, HOLD_TIME_ATTRIBUTE, new Value(holdTime), Index);
		}

		[PublicAPI]
		public void IncrementHoldTime()
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Increment, HOLD_TIME_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementHoldTime()
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Increment, HOLD_TIME_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetHoldIndefinitely(bool holdIndefinitely)
		{
			RequestAttribute(HoldIndefinitelyFeedback, AttributeCode.eCommand.Set, HOLD_INDEFINITELY_ATTRIBUTE, new Value(holdIndefinitely), Index);
		}

		[PublicAPI]
		public void ToggleHoldIndefinitely()
		{
			RequestAttribute(HoldIndefinitelyFeedback, AttributeCode.eCommand.Toggle, HOLD_INDEFINITELY_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		#endregion

		#region Subscription Feedback

		private void HoldEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			HoldEnabled = innerValue.BoolValue;
		}

		private void HoldTimeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			HoldTime = innerValue.FloatValue;
		}

		private void HoldIndefinitelyFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			HoldIndefinitely = innerValue.BoolValue;
		}

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Label = innerValue.StringValue;
		}

		private void LevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Level = innerValue.FloatValue;
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

			addRow("Hold Enabled", HoldEnabled);
			addRow("Hold Time", HoldTime);
			addRow("Hold Indefinitely", HoldIndefinitely);
			addRow("Label", Label);
			addRow("Level", Level);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetHoldEnabled", "SetHoldTime <true/false>", b => SetHoldEnabled(b));
			yield return new ConsoleCommand("ToggleHoldEnabled", "", () => ToggleHoldEnabled());

			yield return new GenericConsoleCommand<float>("SetHoldTime", "SetHoldTime <MS>", f => SetHoldTime(f));
			yield return new ConsoleCommand("IncrementHoldTime", "", () => IncrementHoldTime());
			yield return new ConsoleCommand("DecrementHoldTime", "", () => DecrementHoldTime());

			yield return new GenericConsoleCommand<bool>("SetHoldIndefinitely", "SetHoldIndefinitely <true/false>", b => SetHoldIndefinitely(b));
			yield return new ConsoleCommand("ToggleHoldIndefinitely", "", () => ToggleHoldIndefinitely());

			yield return new GenericConsoleCommand<string>("SetLabel", "SetLabel <LABEL>", s => SetLabel(s));
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
