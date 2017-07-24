using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GatingAutoMixer
{
	public sealed class GatingAutoMixerBlock : AbstractMixerBlock, IVolumeAttributeInterface
	{
		private const string INPUT_COUNT_ATTRIBUTE = "numInputs";
		private const string LOGIC_OUTPUTS_FOLLOW_MIC_LOGIC_ATTRIBUTE = "logicOutputsFollowMicLogic";
		private const string MIC_LOGIC_TYPE_ATTRIBUTE = "micLogic";
		private const string MIX_OUTPUT_LABEL_ATTRIBUTE = "mixOutputLabel";
		private const string OPEN_MIC_LIMIT_ATTRIBUTE = "nomLimit";
		private const string OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE = "nomLimitEnable";
		private const string OUTPUT_LEVEL_ATTRIBUTE = "outputLevel";
		private const string MIN_OUTPUT_LEVEL_ATTRIBUTE = "outputMinLevel";
		private const string MAX_OUTPUT_LEVEL_ATTRIBUTE = "outputMaxLevel";
		private const string OUTPUT_MUTE_ATTRIBUTE = "outputMute";

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnLogicOutputsFollowMicLogicChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnMicLogicChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnMixOutputLabelChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnOpenMicLimitChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnOpenMicLimitEnabledChanged;

		/// <summary>
		/// Raised when the system informs of an input count change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnInputCountChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged; 

		private readonly Dictionary<int, GatingAutoMixerChannel> m_Channels;
		private readonly SafeCriticalSection m_ChannelsSection;

		private bool m_LogicOutputsFollowMicLogic;
		private int m_MicLogic;
		private string m_MixOutputLabel;
		private int m_OpenMicLimit;
		private bool m_OpenMicLimitEnabled;
		private int m_InputCount;
		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;
		private bool m_Mute;

		#region Properties

		[PublicAPI]
		public bool LogicOutputsFollowMicLogic
		{
			get { return m_LogicOutputsFollowMicLogic; }
			private set
			{
				if (value == m_LogicOutputsFollowMicLogic)
					return;

				m_LogicOutputsFollowMicLogic = value;

				Log(eSeverity.Debug, "LogicOutputsFollowMicLogic set to {0}", m_LogicOutputsFollowMicLogic);

				OnLogicOutputsFollowMicLogicChanged.Raise(this, new BoolEventArgs(m_LogicOutputsFollowMicLogic));
			}
		}

		[PublicAPI]
		public int MicLogic
		{
			get { return m_MicLogic; }
			private set
			{
				if (value == m_MicLogic)
					return;

				m_MicLogic = value;

				Log(eSeverity.Debug, "MicLogic set to {0}", m_MicLogic);

				OnMicLogicChanged.Raise(this, new IntEventArgs(m_MicLogic));
			}
		}

		[PublicAPI]
		public string MixOutputLabel
		{
			get { return m_MixOutputLabel; }
			private set
			{
				if (value == m_MixOutputLabel)
					return;

				m_MixOutputLabel = value;

				Log(eSeverity.Debug, "MixOutputLabel set to {0}", m_MixOutputLabel);

				OnMixOutputLabelChanged.Raise(this, new StringEventArgs(m_MixOutputLabel));
			}
		}

		[PublicAPI]
		public int OpenMicLimit
		{
			get { return m_OpenMicLimit; }
			private set
			{
				if (value == m_OpenMicLimit)
					return;

				m_OpenMicLimit = value;

				Log(eSeverity.Debug, "OpenMicLimit set to {0}", m_OpenMicLimit);

				OnOpenMicLimitChanged.Raise(this, new IntEventArgs(m_OpenMicLimit));
			}
		}

		[PublicAPI]
		public bool OpenMicLimitEnabled
		{
			get { return m_OpenMicLimitEnabled; }
			private set
			{
				if (value == m_OpenMicLimitEnabled)
					return;

				m_OpenMicLimitEnabled = value;

				Log(eSeverity.Debug, "OpenMicLimitEnabled set to {0}", m_OpenMicLimitEnabled);

				OnOpenMicLimitEnabledChanged.Raise(this, new BoolEventArgs(m_OpenMicLimitEnabled));
			}
		}

		/// <summary>
		/// Gets the number of inputs.
		/// </summary>
		[PublicAPI]
		public int InputCount
		{
			get { return m_InputCount; }
			private set
			{
				if (value == m_InputCount)
					return;

				m_InputCount = value;

				Log(eSeverity.Debug, "InputCount set to {0}", m_InputCount);

				RebuildInputs();

				OnInputCountChanged.Raise(this, new IntEventArgs(m_InputCount));
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

				Log(eSeverity.Debug, "Level set to {0}", m_Level);

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

				Log(eSeverity.Debug, "MinLevel set to {0}", m_MinLevel);

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

				Log(eSeverity.Debug, "MaxLevel set to {0}", m_MaxLevel);

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

				Log(eSeverity.Debug, "Mute set to {0}", m_Mute);

				OnMuteChanged.Raise(this, new BoolEventArgs(m_Mute));
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
		public GatingAutoMixerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, GatingAutoMixerChannel>();
			m_ChannelsSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnLogicOutputsFollowMicLogicChanged = null;
			OnMicLogicChanged = null;
			OnMixOutputLabelChanged = null;
			OnOpenMicLimitChanged = null;
			OnOpenMicLimitEnabledChanged = null;
			OnInputCountChanged = null;
			OnLevelChanged = null;
			OnMinLevelChanged = null;
			OnMaxLevelChanged = null;
			OnMuteChanged = null;

			base.Dispose();

			DisposeInputs();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(InputCountFeedback, AttributeCode.eCommand.Get, INPUT_COUNT_ATTRIBUTE, null);
			RequestAttribute(LogicOutputsFollowMicLogicFeedback, AttributeCode.eCommand.Get, LOGIC_OUTPUTS_FOLLOW_MIC_LOGIC_ATTRIBUTE, null);
			RequestAttribute(MicLogicTypeFeedback, AttributeCode.eCommand.Get, MIC_LOGIC_TYPE_ATTRIBUTE, null);
			RequestAttribute(MixOutputLabelFeedback, AttributeCode.eCommand.Get, MIX_OUTPUT_LABEL_ATTRIBUTE, null);
			RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Get, OPEN_MIC_LIMIT_ATTRIBUTE, null);
			RequestAttribute(OpenMicLimitEnabledFeedback, AttributeCode.eCommand.Get, OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE, null);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, OUTPUT_MUTE_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the inputs for this mixer block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<GatingAutoMixerChannel> GetInputs()
		{
			return m_ChannelsSection.Execute(() => m_Channels.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the child attribute interface at the given path.
		/// </summary>
		/// <param name="channelType"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public override IAttributeInterface GetAttributeInterface(eChannelType channelType, params int[] indices)
		{
			switch (channelType)
			{
				case eChannelType.Input:
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadInput(indices[0]);
				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		[PublicAPI]
		public void SetLogicOutputsFollowMicLogic(bool follow)
		{
			RequestAttribute(LogicOutputsFollowMicLogicFeedback, AttributeCode.eCommand.Set, LOGIC_OUTPUTS_FOLLOW_MIC_LOGIC_ATTRIBUTE, new Value(follow));
		}

		[PublicAPI]
		public void ToggleLogicOutputsFollowMicLogic()
		{
			RequestAttribute(LogicOutputsFollowMicLogicFeedback, AttributeCode.eCommand.Toggle, LOGIC_OUTPUTS_FOLLOW_MIC_LOGIC_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMicLogic(int micLogic)
		{
			Value value = Value.FromObject(micLogic, GatingAutoMixer.MicLogic.MicLogicToSerial);
			RequestAttribute(MicLogicTypeFeedback, AttributeCode.eCommand.Set, MIC_LOGIC_TYPE_ATTRIBUTE, value);
		}

		[PublicAPI]
		public void SetMicLogic(string micLogicSerial)
		{
			int micLogic = GatingAutoMixer.MicLogic.SerialToMicLogic(micLogicSerial);
			SetMicLogic(micLogic);
		}

		[PublicAPI]
		public void SetMixOutputLabel(string label)
		{
			RequestAttribute(MixOutputLabelFeedback, AttributeCode.eCommand.Set, MIX_OUTPUT_LABEL_ATTRIBUTE, new Value(label));
		}

		[PublicAPI]
		public void SetOpenMicLimit(int limit)
		{
			RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Set, OPEN_MIC_LIMIT_ATTRIBUTE, new Value(limit));
		}

		[PublicAPI]
		public void IncrementOpenMicLimit()
		{
			RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Increment, OPEN_MIC_LIMIT_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementOpenMicLimit()
		{
			RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Decrement, OPEN_MIC_LIMIT_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetOpenMicLimitEnabled(bool enabled)
		{
			RequestAttribute(OpenMicLimitEnabledFeedback, AttributeCode.eCommand.Set, OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE, new Value(enabled));
		}

		[PublicAPI]
		public void ToggleOpenMicLimitEnabled()
		{
			RequestAttribute(OpenMicLimitEnabledFeedback, AttributeCode.eCommand.Toggle, OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMinLevel(float level)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MIN_OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Increment, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMinLevel()
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Decrement, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMaxLevel(float level)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MAX_OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Increment, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMaxLevel()
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Decrement, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMute(bool mute)
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Set, OUTPUT_MUTE_ATTRIBUTE, new Value(mute));
		}

		[PublicAPI]
		public void ToggleMute()
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Toggle, OUTPUT_MUTE_ATTRIBUTE, null);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing inputs and rebuilds from input count.
		/// </summary>
		private void RebuildInputs()
		{
			m_ChannelsSection.Enter();

			try
			{
				Enumerable.Range(1, InputCount).ForEach(i => LazyLoadInput(i));
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}


		private GatingAutoMixerChannel LazyLoadInput(int index)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(index))
					m_Channels[index] = new GatingAutoMixerChannel(this, index);
				return m_Channels[index];
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing inputs.
		/// </summary>
		private void DisposeInputs()
		{
			m_ChannelsSection.Enter();

			try
			{
				m_Channels.Values.ForEach(c => c.Dispose());
				m_Channels.Clear();
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		#endregion

		#region Subscription Callbacks

		private void InputCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			InputCount = innerValue.IntValue;
		}

		private void LogicOutputsFollowMicLogicFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LogicOutputsFollowMicLogic = innerValue.BoolValue;
		}

		private void MicLogicTypeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MicLogic = innerValue.GetObjectValue(s => GatingAutoMixer.MicLogic.SerialToMicLogic(s));
		}

		private void MixOutputLabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MixOutputLabel = innerValue.StringValue;
		}

		private void OpenMicLimitFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			OpenMicLimit = innerValue.IntValue;
		}

		private void OpenMicLimitEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			OpenMicLimitEnabled = innerValue.BoolValue;
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

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Logic Outputs Follow Mic Logic", LogicOutputsFollowMicLogic);
			addRow("Mic Logic", GatingAutoMixer.MicLogic.MicLogicToSerial(MicLogic));
			addRow("Mix Output Label", MixOutputLabel);
			addRow("Open Mic Limit", OpenMicLimit);
			addRow("Open Mic Limit Enabled", OpenMicLimitEnabled);
			addRow("Input Count", InputCount);
			addRow("Output Level", Level);
			addRow("Min Output Level", MinLevel);
			addRow("Max Output Level", MaxLevel);
			addRow("Output Mute", Mute);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("Inputs", GetInputs(), i => (uint)i.Index);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetLogicOutputsFollowMicLogic",
			                                             "SetLogicOutputsFollowMicLogic <true/false>",
			                                             b => SetLogicOutputsFollowMicLogic(b));
			yield return new ConsoleCommand("ToggleLogicOutputsFollowMicLogic", "", () => ToggleLogicOutputsFollowMicLogic());

			string setMicLogicHelp =
				string.Format("SetMicLogic <[{0}, {1}, {2}1, {2}2, ...]>",
				              GatingAutoMixer.MicLogic.MIC_LOGIC_NONE_SERIAL,
				              GatingAutoMixer.MicLogic.MIC_LOGIC_LASTHOLD_SERIAL,
				              GatingAutoMixer.MicLogic.MIC_LOGIC_CHAN_PREFIX);
			yield return new GenericConsoleCommand<string>("SetMicLogic", setMicLogicHelp, s => SetMicLogic(s));

			yield return new GenericConsoleCommand<string>("SetMixOutputLabel", "SetMixOutputLabel <LABEL>", s => SetMixOutputLabel(s));

			yield return new GenericConsoleCommand<int>("SetOpenMicLimit", "SetOpenMicLimit <COUNT>", i => SetOpenMicLimit(i));
			yield return new ConsoleCommand("IncrementOpenMicLimit", "", () => IncrementOpenMicLimit());
			yield return new ConsoleCommand("DecrementOpenMicLimit", "", () => DecrementOpenMicLimit());

			yield return new GenericConsoleCommand<bool>("SetOpenMicLimitEnabled", "SetOpenMicLimitEnabled <true/false>",
														 b => SetOpenMicLimitEnabled(b));
			yield return new ConsoleCommand("ToggleOpenMicLimitEnabled", "", () => ToggleOpenMicLimitEnabled());

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <LEVEL>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "", () => IncrementOpenMicLimit());
			yield return new ConsoleCommand("DecrementLevel", "", () => DecrementOpenMicLimit());

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
