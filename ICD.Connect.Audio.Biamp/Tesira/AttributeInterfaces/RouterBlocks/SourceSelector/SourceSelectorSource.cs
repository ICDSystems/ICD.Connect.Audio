using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.RouterBlocks.SourceSelector
{
	public sealed class SourceSelectorSource : AbstractAttributeChild<SourceSelectorBlock>
	{
		private const string LABEL_ATTRIBUTE = "label";
		private const string LEVEL_ATTRIBUTE = "sourceLevel";
		private const string MIN_LEVEL_ATTRIBUTE = "sourceMinLevel";
		private const string MAX_LEVEL_ATTRIBUTE = "sourceMaxLevel";

		/// <summary>
		/// Raised when the system informs of a label change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnLabelChanged;

		/// <summary>
		/// Raised when the system informs of a level change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		/// <summary>
		/// Raised when the system informs of a min level change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;

		/// <summary>
		/// Raised when the system informs of a max level change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged; 

		private string m_Label;
		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;

		#region Properties

		/// <summary>
		/// Gets the cached label.
		/// </summary>
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

		/// <summary>
		/// Gets the cached level.
		/// </summary>
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
		/// Gets the cached min level.
		/// </summary>
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

		/// <summary>
		/// Gets the cached max level.
		/// </summary>
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


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Source"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public SourceSelectorSource(SourceSelectorBlock parent, int index)
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
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			// Subscribe
			RequestAttribute(LevelFeedback, command, LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the label of the source.
		/// </summary>
		/// <param name="label"></param>
		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		/// <summary>
		/// Sets the level of the source.
		/// </summary>
		/// <param name="level"></param>
		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		/// <summary>
		/// Increments the level of the source.
		/// </summary>
		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Decerements the level of the source.
		/// </summary>
		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the min level of the source.
		/// </summary>
		/// <param name="minLevel"></param>
		[PublicAPI]
		public void SetMinLevel(float minLevel)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MIN_LEVEL_ATTRIBUTE, new Value(minLevel), Index);
		}

		/// <summary>
		/// Increments the min level of the source.
		/// </summary>
		[PublicAPI]
		public void IncrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Increment, MIN_LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Decerements the min level of the source.
		/// </summary>
		[PublicAPI]
		public void DecrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Decrement, MIN_LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the max level of the source.
		/// </summary>
		/// <param name="maxLevel"></param>
		[PublicAPI]
		public void SetMaxLevel(float maxLevel)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MAX_LEVEL_ATTRIBUTE, new Value(maxLevel), Index);
		}

		/// <summary>
		/// Increments the min level of the source.
		/// </summary>
		[PublicAPI]
		public void IncrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Increment, MAX_LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Decerements the min level of the source.
		/// </summary>
		[PublicAPI]
		public void DecrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Decrement, MAX_LEVEL_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Label = innerValue.StringValue;
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

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
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

			addRow("Label", Label);
			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
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
			yield return new ConsoleCommand("IncrementLevel", "Increments the source level", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "Decrements the source level", () => DecrementLevel());

			yield return new GenericConsoleCommand<float>("SetMinLevel", "SetMinLevel <LEVEL>", f => SetMinLevel(f));
			yield return new ConsoleCommand("IncrementMinLevel", "Increments the min source level", () => IncrementMinLevel());
			yield return new ConsoleCommand("DecrementMinLevel", "Decrements the min source level", () => DecrementMinLevel());

			yield return new GenericConsoleCommand<float>("SetMaxLevel", "SetMaxLevel <LEVEL>", f => SetMaxLevel(f));
			yield return new ConsoleCommand("IncrementMaxLevel", "Increments the max source level", () => IncrementMaxLevel());
			yield return new ConsoleCommand("DecrementMaxLevel", "Decrements the max source level", () => DecrementMaxLevel());
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
