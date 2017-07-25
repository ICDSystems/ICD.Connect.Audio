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

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GainSharingAutoMixer
{
	public sealed class GainSharingAutoMixerChannel :
		AbstractAttributeChild<GainSharingAutoMixerBlock>, IVolumeAttributeInterface
	{
		private const string LEVEL_ATTRIBUTE = "channelLevel";
		private const string MIN_LEVEL_ATTRIBUTE = "channelMinLevel";
		private const string MAX_LEVEL_ATTRIBUTE = "channelMaxLevel";
		private const string MUTE_ATTRIBUTE = "channelMute";
		private const string CROSSPOINT_ON_ATTRIBUTE = "crosspoint";
		private const string GAIN_REDUCTION_ATTRIBUTE = "gainReduction";
		private const string INPUT_LABEL_ATTRIBUTE = "inputLabel";
		private const string INPUT_MUTE_ATTRIBUTE = "inputMute";

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnCrosspointOnChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnGainReductionChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnInputLabelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnInputMuteChanged; 

		private float m_Level;
		private float m_MinLevel;
		private float m_MaxLevel;
		private bool m_Mute;
		private bool m_CrosspointOn;
		private float m_GainReduction;
		private string m_InputLabel;
		private bool m_InputMute;

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
			get { return m_MaxLevel; }
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
		public bool CrosspointOn
		{
			get { return m_CrosspointOn; }
			private set
			{
				if (value == m_CrosspointOn)
					return;

				m_CrosspointOn = value;

				Log(eSeverity.Informational, "CrosspointOn set to {0}", m_CrosspointOn);

				OnCrosspointOnChanged.Raise(this, new BoolEventArgs(m_CrosspointOn));
			}
		}

		[PublicAPI]
		public float GainReduction
		{
			get { return m_GainReduction; }
			private set
			{
				if (Math.Abs(value - m_GainReduction) < 0.01f)
					return;

				m_GainReduction = value;

				Log(eSeverity.Informational, "GainReduction set to {0}", m_GainReduction);

				OnGainReductionChanged.Raise(this, new FloatEventArgs(m_GainReduction));
			}
		}

		[PublicAPI]
		public string InputLabel
		{
			get { return m_InputLabel; }
			private set
			{
				if (value == m_InputLabel)
					return;

				m_InputLabel = value;

				Log(eSeverity.Informational, "InputLabel set to {0}", m_InputLabel);

				OnInputLabelChanged.Raise(this, new StringEventArgs(m_InputLabel));
			}
		}

		[PublicAPI]
		public bool InputMute
		{
			get { return m_InputMute; }
			private set
			{
				if (value == m_InputMute)
					return;

				m_InputMute = value;

				Log(eSeverity.Informational, "InputMute set to {0}", m_InputMute);

				OnInputMuteChanged.Raise(this, new BoolEventArgs(m_InputMute));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public GainSharingAutoMixerChannel(GainSharingAutoMixerBlock parent, int index)
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
			OnCrosspointOnChanged = null;
			OnGainReductionChanged = null;
			OnInputLabelChanged = null;
			OnInputMuteChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Unsubscribe, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Unsubscribe, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Unsubscribe, CROSSPOINT_ON_ATTRIBUTE, null, Index);
			RequestAttribute(GainReductionFeedback, AttributeCode.eCommand.Unsubscribe, GAIN_REDUCTION_ATTRIBUTE, null, Index);
			RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Unsubscribe, INPUT_MUTE_ATTRIBUTE, null, Index);
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
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Get, CROSSPOINT_ON_ATTRIBUTE, null, Index);
			RequestAttribute(GainReductionFeedback, AttributeCode.eCommand.Get, GAIN_REDUCTION_ATTRIBUTE, null, Index);
			RequestAttribute(InputLabelFeedback, AttributeCode.eCommand.Get, INPUT_LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Get, INPUT_MUTE_ATTRIBUTE, null, Index);

			// Subscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Subscribe, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Subscribe, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Subscribe, CROSSPOINT_ON_ATTRIBUTE, null, Index);
			RequestAttribute(GainReductionFeedback, AttributeCode.eCommand.Subscribe, GAIN_REDUCTION_ATTRIBUTE, null, Index);
			RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Subscribe, INPUT_MUTE_ATTRIBUTE, null, Index);
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
		public void SetMinLevel(float minLevel)
		{
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Set, MIN_LEVEL_ATTRIBUTE, new Value(minLevel), Index);
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
		public void SetMaxLevel(float maxLevel)
		{
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Set, MAX_LEVEL_ATTRIBUTE, new Value(maxLevel), Index);
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
		public void SetCrosspointOn(bool crosspointOn)
		{
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Set, CROSSPOINT_ON_ATTRIBUTE, new Value(crosspointOn), Index);
		}

		[PublicAPI]
		public void ToggleCrosspointOn()
		{
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Toggle, CROSSPOINT_ON_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetInputLabel(string label)
		{
			RequestAttribute(InputLabelFeedback, AttributeCode.eCommand.Set, INPUT_LABEL_ATTRIBUTE, new Value(label), Index);
		}

		[PublicAPI]
		public void SetInputMute(bool mute)
		{
			RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Set, INPUT_MUTE_ATTRIBUTE, new Value(mute), Index);
		}

		[PublicAPI]
		public void ToggleInputMute()
		{
			RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Toggle, INPUT_MUTE_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Private Methods

		private void InputLabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			InputLabel = innerValue.StringValue;
		}

		private void MaxLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MaxLevel = innerValue.FloatValue;
		}

		private void MinLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MinLevel = innerValue.FloatValue;
		}

		private void InputMuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			InputMute = innerValue.BoolValue;
		}

		private void GainReductionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			GainReduction = innerValue.FloatValue;
		}

		private void CrosspointOnFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			CrosspointOn = innerValue.BoolValue;
		}

		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Mute = innerValue.BoolValue;
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

			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
			addRow("Mute", Mute);
			addRow("Crosspoint On", CrosspointOn);
			addRow("Gain Reduction", GainReduction);
			addRow("Input Label", InputLabel);
			addRow("Input Mute", InputMute);
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

			yield return new GenericConsoleCommand<bool>("SetCrosspointOn", "SetCrosspointOn <true/false>", b => SetCrosspointOn(b));
			yield return new ConsoleCommand("ToggleCrosspointOn", "", () => ToggleCrosspointOn());

			yield return new GenericConsoleCommand<string>("SetInputLabel", "SetInputLabel <LABEL>", s => SetInputLabel(s));

			yield return new GenericConsoleCommand<bool>("SetInputMute", "SetInputMute <true/false>", b => SetInputMute(b));
			yield return new ConsoleCommand("ToggleInputMute", "", () => ToggleInputMute());
		}

		/// <summary>
		/// Workaround for 
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
