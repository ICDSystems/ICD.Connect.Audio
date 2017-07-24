using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface
{
	public sealed class TiReceiveBlock : AbstractIoBlock, IVolumeAttributeInterface
	{
		private const string LINE_ECHO_CANCEL_ATTRIBUTE = "lec";
		private const string INPUT_LEVEL_ATTRIBUTE = "level";
		private const string MAX_INPUT_INPUT_LEVEL_ATTRIBUTE = "maxLevel";
		private const string MIN_INPUT_INPUT_LEVEL_ATTRIBUTE = "minLevel";
		private const string MUTE_ATTRIBUTE = "mute";
		private const string RING_TONE_LEVEL_ATTRIBUTE = "ringLevel";

		public event EventHandler<BoolEventArgs> OnLineEchoCancelChanged; 
		public event EventHandler<FloatEventArgs> OnLevelChanged;
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;
		public event EventHandler<BoolEventArgs> OnMuteChanged;
		public event EventHandler<FloatEventArgs> OnRingToneLevelChanged;

		private bool m_LineEchoCancel;
		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;
		private bool m_Mute;
		private float m_RingToneLevel;

		#region Properties

		[PublicAPI]
		public bool LineEchoCancel
		{
			get { return m_LineEchoCancel; }
			private set
			{
				if (value == m_LineEchoCancel)
					return;

				m_LineEchoCancel = value;

				OnLineEchoCancelChanged.Raise(this, new BoolEventArgs(m_LineEchoCancel));
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

				OnMuteChanged.Raise(this, new BoolEventArgs(m_Mute));
			}
		}

		[PublicAPI]
		public float RingToneLevel
		{
			get { return m_RingToneLevel; }
			private set
			{
				if (Math.Abs(value - m_RingToneLevel) < 0.01f)
					return;

				m_RingToneLevel = value;

				OnRingToneLevelChanged.Raise(this, new FloatEventArgs(m_RingToneLevel));
			}
		}

		public float AttributeMinLevel { get { return -100.0f; } }
		public float AttributeMaxLevel { get { return 12.0f; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public TiReceiveBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnLineEchoCancelChanged = null;
			OnLevelChanged = null;
			OnMinLevelChanged = null;
			OnMaxLevelChanged = null;
			OnMuteChanged = null;
			OnRingToneLevelChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Unsubscribe, MUTE_ATTRIBUTE, null);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LineEchoCancelFeedback, AttributeCode.eCommand.Get, LINE_ECHO_CANCEL_ATTRIBUTE, null);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, INPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null);
			RequestAttribute(RingToneLevelFeedback, AttributeCode.eCommand.Get, RING_TONE_LEVEL_ATTRIBUTE, null);

			// Subscribe
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Subscribe, MUTE_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetLineEchoCancel(bool cancel)
		{
			RequestAttribute(LineEchoCancelFeedback, AttributeCode.eCommand.Set, LINE_ECHO_CANCEL_ATTRIBUTE, new Value(cancel));
		}

		[PublicAPI]
		public void ToggleLineEchoCancel()
		{
			RequestAttribute(LineEchoCancelFeedback, AttributeCode.eCommand.Toggle, LINE_ECHO_CANCEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, INPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMinLevel(float level)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MIN_INPUT_INPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Increment, MIN_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Decrement, MIN_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMaxLevel(float level)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MAX_INPUT_INPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Increment, MAX_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Decrement, MAX_INPUT_INPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMute(bool mute)
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Set, MUTE_ATTRIBUTE, new Value(mute));
		}

		[PublicAPI]
		public void ToggleMute()
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Toggle, MUTE_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetRingToneLevel(float level)
		{
			RequestAttribute(RingToneLevelFeedback, AttributeCode.eCommand.Set, RING_TONE_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementRingToneLevel()
		{
			RequestAttribute(RingToneLevelFeedback, AttributeCode.eCommand.Increment, RING_TONE_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementRingToneLevel()
		{
			RequestAttribute(RingToneLevelFeedback, AttributeCode.eCommand.Decrement, RING_TONE_LEVEL_ATTRIBUTE, null);
		}

		#endregion

		#region Subscription Callbacks

		private void LineEchoCancelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LineEchoCancel = innerValue.BoolValue;
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

		private void RingToneLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			RingToneLevel = innerValue.FloatValue;
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

			addRow("Line Echo Cancel", LineEchoCancel);
			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
			addRow("Mute", Mute);
			addRow("Ring Tone Level", RingToneLevel);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetLineEchoCancel", "SetLineEchoCancel <true/false>", b => SetLineEchoCancel(b));
			yield return new ConsoleCommand("ToggleLineEchoCancel", "", () => ToggleLineEchoCancel());

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <LEVEL>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "", () => DecrementLevel());

			yield return new GenericConsoleCommand<float>("SetMinLevel", "SetMinLevel <LEVEL>", f => SetMinLevel(f));
			yield return new ConsoleCommand("IncrementMinLevel", "", () => IncrementMinLevel());
			yield return new ConsoleCommand("DecrementMinLevel", "", () => DecrementMinLevel());

			yield return new GenericConsoleCommand<float>("SetMaxLevel", "SetMaxLevel <LEVEL>", l => SetMaxLevel(l));
			yield return new ConsoleCommand("IncrementMaxLevel", "", () => IncrementMaxLevel());
			yield return new ConsoleCommand("DecrementMaxLevel", "", () => DecrementMaxLevel());

			yield return new GenericConsoleCommand<bool>("SetMute", "SetMute <true/false>", m => SetMute(m));
			yield return new ConsoleCommand("ToggleMute", "", () => ToggleMute());

			yield return new GenericConsoleCommand<float>("SetRingToneLevel", "SetRingToneLevel <LEVEL>", f => SetRingToneLevel(f));
			yield return new ConsoleCommand("IncrementRingToneLevel", "", () => IncrementRingToneLevel());
			yield return new ConsoleCommand("DecrementRingToneLevel", "", () => DecrementRingToneLevel());
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
