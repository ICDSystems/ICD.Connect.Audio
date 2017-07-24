using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.StandardMixer
{
	public abstract class AbstractStandardMixerIo : AbstractAttributeChild<StandardMixerBlock>, IVolumeAttributeInterface
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged;

		/// <summary>
		/// Raised when the label changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnLabelChanged;

		/// <summary>
		/// Raised when the level changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		/// <summary>
		/// Raised when the min level changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;

		/// <summary>
		/// Raised when the max level changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;

		private bool m_Mute;
		private string m_Label;
		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;

		#region Properties

		protected abstract string LabelAttribute { get; }
		protected abstract string LevelAttribute { get; }
		protected abstract string MinLevelAttribute { get; }
		protected abstract string MaxLevelAttribute { get; }
		protected abstract string MuteAttribute { get; }

		/// <summary>
		/// Gets the mute state.
		/// </summary>
		[PublicAPI]
		public bool Mute
		{
			get { return m_Mute; }
			private set
			{
				if (value == m_Mute)
					return;

				m_Mute = value;

				OnMuteChanged.Raise(this, new BoolEventArgs(m_Mute));
			}
		}

		public float AttributeMinLevel { get { return -100.0f; } }

		public float AttributeMaxLevel { get { return 12.0f; } }

		/// <summary>
		/// Gets the label.
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

				OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
			}
		}

		/// <summary>
		/// Gets the level.
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

				OnMinLevelChanged.Raise(this, new FloatEventArgs(m_Level));
			}
		}

		/// <summary>
		/// Gets the min level.
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

				OnMinLevelChanged.Raise(this, new FloatEventArgs(m_MinLevel));
			}
		}

		/// <summary>
		/// Gets the max level.
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

				OnMaxLevelChanged.Raise(this, new FloatEventArgs(m_MaxLevel));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		protected AbstractStandardMixerIo(StandardMixerBlock parent, int index)
			: base(parent, index)
		{
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnMuteChanged = null;
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

			// Get initial state
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MuteAttribute, null, Index);
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LabelAttribute, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LevelAttribute, null, Index);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MinLevelAttribute, null, Index);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MaxLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void SetMute(bool mute)
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Set, MuteAttribute, new Value(mute), Index);
		}

		[PublicAPI]
		public void ToggleMute()
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Toggle, MuteAttribute, null, Index);
		}

		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LabelAttribute, new Value(label), Index);
		}

		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, LevelAttribute, new Value(level), Index);
		}

		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, LevelAttribute, null, Index);
		}

		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, LevelAttribute, null, Index);
		}

		[PublicAPI]
		public void SetMinLevel(float level)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MinLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void IncrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Increment, MinLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void DecrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Decrement, MinLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void SetMaxLevel(float level)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MaxLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void IncrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Increment, MaxLevelAttribute, null, Index);
		}

		[PublicAPI]
		public void DecrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Decrement, MaxLevelAttribute, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Mute = innerValue.BoolValue;
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

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Mute", Mute);
			addRow("Label", Label);
			addRow("Level", Level);
			addRow("MinLevel", MinLevel);
			addRow("MaxLevel", MaxLevel);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetMute", "SetMute <true/false>", b => SetMute(b));
			yield return new ConsoleCommand("ToggleMute", "Toggles the current mute state", () => ToggleMute());

			yield return new GenericConsoleCommand<string>("SetLabel", "SetLabel <LABEL>", s => SetLabel(s));

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <LEVEL>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "Increments the level", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "Decrements the level", () => DecrementLevel());

			yield return new GenericConsoleCommand<float>("SetMinLevel", "SetMinLevel <LEVEL>", f => SetMinLevel(f));
			yield return new ConsoleCommand("IncrementMinLevel", "Increments the min level", () => IncrementMinLevel());
			yield return new ConsoleCommand("DecrementMinLevel", "Decrements the min level", () => DecrementMinLevel());

			yield return new GenericConsoleCommand<float>("SetMaxLevel", "SetMaxLevel <LEVEL>", f => SetMaxLevel(f));
			yield return new ConsoleCommand("IncrementMaxLevel", "Increments the max level", () => IncrementMaxLevel());
			yield return new ConsoleCommand("DecrementMaxLevel", "Decrements the max level", () => DecrementMaxLevel());
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
