using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks
{
	public sealed class LevelControlChannel : AbstractAttributeChild<LevelControlBlock>, IVolumeAttributeInterface
	{
		private const string LABEL_ATTRIBUTE = "label";
		private const string LEVEL_ATTRIBUTE = "level";
		private const string MIN_LEVEL_ATTRIBUTE = "minLevel";
		private const string MAX_LEVEL_ATTRIBUTE = "maxLevel";
		private const string MUTE_ATTRIBUTE = "mute";
		private const string RAMP_INTERVAL_ATTRIBUTE = "rampInterval";
		private const string RAMP_STEP_ATTRIBUTE = "rampStep";

		public event EventHandler<StringEventArgs> OnLabelChanged;
		public event EventHandler<FloatEventArgs> OnLevelChanged;
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;
		public event EventHandler<BoolEventArgs> OnMuteChanged;
		public event EventHandler<FloatEventArgs> OnRampIntervalChanged;
		public event EventHandler<FloatEventArgs> OnRampStepChanged;

		private string m_Label;
		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;
		private bool m_Mute;
		private float m_RampInterval;
		private float m_RampStep;

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

		[PublicAPI]
		public float MinLevel
		{
			get { return m_MinLevel; }
			private set
			{
				if (Math.Abs(value - m_MinLevel) < 0.01f)
					return;

				m_MinLevel = value;

				Log(eSeverity.Informational, "MinLevel set to {0}", m_MinLevel);

				OnMinLevelChanged.Raise(this, new FloatEventArgs(m_MinLevel));
			}
		}

		[PublicAPI]
		public float MaxLevel
		{
			get { return m_MaxLevel; }
			private set
			{
				if (Math.Abs(value - m_MaxLevel) < 0.01f)
					return;

				m_MaxLevel = value;

				Log(eSeverity.Informational, "MaxLevel set to {0}", m_MaxLevel);

				OnMaxLevelChanged.Raise(this, new FloatEventArgs(m_MaxLevel));
			}
		}

		[PublicAPI]
		public bool Mute
		{
			get { return m_Mute; }
			private set
			{
				if (value == m_Mute)
					return;

				m_Mute = value;

				Log(eSeverity.Informational, "Mute set to {0}", m_Mute);

				OnMuteChanged.Raise(this, new BoolEventArgs(m_Mute));
			}
		}

		public float AttributeMinLevel { get { return -100.0f; } }
		public float AttributeMaxLevel { get { return 12.0f; } }

		[PublicAPI]
		public float RampInterval
		{
			get { return m_RampInterval; }
			private set
			{
				if (Math.Abs(value - m_RampInterval) < 0.01f)
					return;

				m_RampInterval = value;

				Log(eSeverity.Informational, "RampInterval set to {0}", m_RampInterval);

				OnRampIntervalChanged.Raise(this, new FloatEventArgs(m_RampInterval));
			}
		}

		[PublicAPI]
		public float RampStep
		{
			get { return m_RampStep; }
			private set
			{
				if (Math.Abs(value - m_RampStep) < 0.01f)
					return;

				m_RampStep = value;

				Log(eSeverity.Informational, "RampStep set to {0}", m_RampStep);

				OnRampStepChanged.Raise(this, new FloatEventArgs(m_RampStep));
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
		public LevelControlChannel(LevelControlBlock parent, int index)
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
			OnLevelChanged = null;
			OnMinLevelChanged = null;
			OnMaxLevelChanged = null;
			OnMuteChanged = null;
			OnRampIntervalChanged = null;
			OnRampStepChanged = null;

			base.Dispose();
		}

        public override void Deinitialize()
        {
            base.Deinitialize();

            RequestAttribute(MuteFeedback, AttributeCode.eCommand.Unsubscribe, MUTE_ATTRIBUTE, null, Index);
            RequestAttribute(LevelFeedback, AttributeCode.eCommand.Unsubscribe, LEVEL_ATTRIBUTE, null, Index);
        }

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(RampIntervalFeedback, AttributeCode.eCommand.Get, RAMP_INTERVAL_ATTRIBUTE, null, Index);
			RequestAttribute(RampStepFeedback, AttributeCode.eCommand.Get, RAMP_STEP_ATTRIBUTE, null, Index);

			// Subscribe
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Subscribe, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Subscribe, LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMinLevel(float level)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MIN_LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		[PublicAPI]
		public void IncrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Increment, MIN_LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Decrement, MIN_LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMaxLevel(float level)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MAX_LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		[PublicAPI]
		public void IncrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Increment, MAX_LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Decrement, MAX_LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMute(bool mute)
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Set, MUTE_ATTRIBUTE, new Value(mute), Index);
		}

		[PublicAPI]
		public void ToggleMute()
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Toggle, MUTE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetRampInterval(float rampInterval)
		{
			RequestAttribute(RampIntervalFeedback, AttributeCode.eCommand.Set, RAMP_INTERVAL_ATTRIBUTE, new Value(rampInterval), Index);
		}

		[PublicAPI]
		public void IncrementRampInterval()
		{
			RequestAttribute(RampIntervalFeedback, AttributeCode.eCommand.Increment, RAMP_INTERVAL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementRampInterval()
		{
			RequestAttribute(RampIntervalFeedback, AttributeCode.eCommand.Decrement, RAMP_INTERVAL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetRampStep(float rampStep)
		{
			RequestAttribute(RampStepFeedback, AttributeCode.eCommand.Set, RAMP_STEP_ATTRIBUTE, new Value(rampStep), Index);
		}

		[PublicAPI]
		public void IncrementRampStep()
		{
			RequestAttribute(RampStepFeedback, AttributeCode.eCommand.Increment, RAMP_STEP_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementRampStep()
		{
			RequestAttribute(RampStepFeedback, AttributeCode.eCommand.Decrement, RAMP_STEP_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

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

		private void MinLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MinLevel = innerValue.FloatValue;
		}

		private void MaxLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MaxLevel = innerValue.FloatValue;
		}

		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Mute = innerValue.BoolValue;
		}

		private void RampIntervalFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			RampInterval = innerValue.FloatValue;
		}

		private void RampStepFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			RampStep = innerValue.FloatValue;
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
			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
			addRow("Mute", Mute);
			addRow("Ramp Interval", RampInterval);
			addRow("Ramp Step", RampStep);
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

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <LEVEL>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "", () => DecrementLevel());

			yield return new GenericConsoleCommand<float>("SetMinLevel", "SetMinLevel <LEVEL>", f => SetMinLevel(f));
			yield return new ConsoleCommand("IncrementMinLevel", "", () => IncrementMinLevel());
			yield return new ConsoleCommand("DecrementMinLevel", "", () => DecrementMinLevel());

			yield return new GenericConsoleCommand<float>("SetMaxLevel", "SetMaxLevel <LEVEL>", f => SetMaxLevel(f));
			yield return new ConsoleCommand("IncrementMaxLevel", "", () => IncrementMaxLevel());
			yield return new ConsoleCommand("DecrementMaxLevel", "", () => DecrementMaxLevel());

			yield return new GenericConsoleCommand<bool>("SetMute", "SetMute <true/false>", b => SetMute(b));
			yield return new ConsoleCommand("ToggleMute", "", () => ToggleMute());

			yield return new GenericConsoleCommand<float>("SetRampInterval", "SetRampInterval <INTERVAL>", f => SetRampInterval(f));
			yield return new ConsoleCommand("IncrementRampInterval", "", () => IncrementRampInterval());
			yield return new ConsoleCommand("DecrementRampInterval", "", () => DecrementRampInterval());

			yield return new GenericConsoleCommand<float>("SetRampStep", "SetRampStep <STEP>", f => SetRampStep(f));
			yield return new ConsoleCommand("IncrementRampStep", "", () => IncrementRampStep());
			yield return new ConsoleCommand("DecrementRampStep", "", () => DecrementRampStep());
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
