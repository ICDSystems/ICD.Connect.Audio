using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp
{
	public sealed class VoIpTransmitLine : AbstractAttributeChild<VoIpTransmitBlock>, IVolumeAttributeInterface
	{
		private const string LEVEL_ATTRIBUTE = "level";
		private const string MIN_LEVEL_ATTRIBUTE = "minLevel";
		private const string MAX_LEVEL_ATTRIBUTE = "maxLevel";
		private const string MUTE_ATTRIBUTE = "mute";

		public event EventHandler<FloatEventArgs> OnLevelChanged;
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;
		public event EventHandler<BoolEventArgs> OnMuteChanged;

		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;
		private bool m_Mute;

		#region Properties

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

		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Line"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public VoIpTransmitLine(VoIpTransmitBlock parent, int index)
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
			OnLevelChanged = null;
			OnMinLevelChanged = null;
			OnMaxLevelChanged = null;
			OnMuteChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		[PublicAPI]
		public void IncrementLevel()
		{
			IncrementLevel(DEFAULT_INCREMENT_VALUE);
		}

		[PublicAPI]
		public void DecrementLevel()
		{
			DecrementLevel(DEFAULT_INCREMENT_VALUE);
		}

		[PublicAPI]
		public void IncrementLevel(float incrementValue)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, LEVEL_ATTRIBUTE, new Value(incrementValue), Index);
		}

		[PublicAPI]
		public void DecrementLevel(float decrementValue)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, LEVEL_ATTRIBUTE, new Value(decrementValue), Index);
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

		#endregion

		#region Subscription Callbacks

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

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
			addRow("Mute", Mute);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

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
