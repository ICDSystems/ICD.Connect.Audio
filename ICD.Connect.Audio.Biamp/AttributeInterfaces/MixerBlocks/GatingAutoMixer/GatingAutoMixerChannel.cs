using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GatingAutoMixer
{
	public sealed class GatingAutoMixerChannel : AbstractAttributeChild<GatingAutoMixerBlock>, IVolumeAttributeInterface
	{
		public enum eDirectOutput
		{
			PostGatePreNom,
			PostGatePostNom
		}

		private static readonly Dictionary<string, eDirectOutput> s_DirectOutputModes
			= new Dictionary<string, eDirectOutput>(StringComparer.OrdinalIgnoreCase)
			{
				{"POST_GATE_PRE_NOM", eDirectOutput.PostGatePreNom},
				{"POST_GATE_POST_NOM", eDirectOutput.PostGatePostNom}
			};

		private const string CROSSPOINT_ON_ATTRIBUTE = "crosspoint";
		private const string DIRECT_OUTPUT_ATTRIBUTE = "directOutputLogic";
		private const string GATE_HOLD_TIME_ATTRIBUTE = "gateHoldTimeMs";
		private const string LABEL_ATTRIBUTE = "inputLabel";
		private const string LEVEL_ATTRIBUTE = "inputLevel";
		private const string MAX_LEVEL_ATTRIBUTE = "inputMaxLevel";
		private const string MIN_LEVEL_ATTRIBUTE = "inputMinLevel";
		private const string MUTE_ATTRIBUTE = "inputMute";
		private const string MANUAL_ATTRIBUTE = "manual";
		private const string NOM_GAIN_ENABLED_ATTRIBUTE = "nomGainEnable";
		private const string OFF_ATTENUATION_ATTRIBUTE = "offGain";

		public delegate void DirectOutputCallback(GatingAutoMixerChannel sender, eDirectOutput directOutput);

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnCrosspointOnChanged;

		[PublicAPI]
		public event DirectOutputCallback OnDirectOutputChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnGateHoldTimeChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnLogicOutputInvertChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnLabelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinLevelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnManualChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnNomGainEnabledChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnOffAttenuationChanged; 

		private bool m_CrosspointOn;
		private eDirectOutput m_DirectOutput;
		private float m_GateHoldTime;
		private string m_Label;
		private float m_Level;
		private float m_MaxLevel;
		private float m_MinLevel;
		private bool m_Mute;
		private bool m_Manual;
		private bool m_NomGainEnabled;
		private float m_OffAttenuation;

		#region Properties

		[PublicAPI]
		public bool CrosspointOn
		{
			get { return m_CrosspointOn; }
			private set
			{
				if (value == m_CrosspointOn)
					return;

				m_CrosspointOn = value;

				OnCrosspointOnChanged.Raise(this, new BoolEventArgs(m_CrosspointOn));
			}
		}

		[PublicAPI]
		public eDirectOutput DirectOutput
		{
			get { return m_DirectOutput; }
			private set
			{
				if (value == m_DirectOutput)
					return;

				m_DirectOutput = value;

				DirectOutputCallback handler = OnDirectOutputChanged;
				if (handler != null)
					handler(this, m_DirectOutput);
			}
		}

		[PublicAPI]
		public float GateHoldTime
		{
			get { return m_GateHoldTime; }
			private set
			{
				if (Math.Abs(value - m_GateHoldTime) < 0.01f)
					return;

				m_GateHoldTime = value;

				OnGateHoldTimeChanged.Raise(this, new FloatEventArgs(m_GateHoldTime));
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

				OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
			}
		}

		[PublicAPI]
		public float Level
		{
			get { return m_MaxLevel; }
			private set
			{
				if (Math.Abs(value - m_Level) < 0.01f)
					return;

				m_Level = value;

				OnLevelChanged.Raise(this, new FloatEventArgs(m_Level));
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

		[PublicAPI]
		public bool Manual
		{
			get { return m_Manual; }
			private set
			{
				if (value == m_Manual)
					return;

				m_Manual = value;

				OnManualChanged.Raise(this, new BoolEventArgs(m_Manual));
			}
		}

		[PublicAPI]
		public bool NomGainEnabled
		{
			get { return m_NomGainEnabled; }
			private set
			{
				if (value == m_NomGainEnabled)
					return;

				m_NomGainEnabled = value;

				OnNomGainEnabledChanged.Raise(this, new BoolEventArgs(m_NomGainEnabled));
			}
		}

		[PublicAPI]
		public float OffAttenuation
		{
			get { return m_OffAttenuation; }
			private set
			{
				if (Math.Abs(value - m_OffAttenuation) < 0.01f)
					return;

				m_OffAttenuation = value;

				OnOffAttenuationChanged.Raise(this, new FloatEventArgs(m_OffAttenuation));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public GatingAutoMixerChannel(GatingAutoMixerBlock parent, int index)
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
			OnCrosspointOnChanged = null;
			OnDirectOutputChanged = null;
			OnGateHoldTimeChanged = null;
			OnLogicOutputInvertChanged = null;
			OnLabelChanged = null;
			OnLevelChanged = null;
			OnMaxLevelChanged = null;
			OnMinLevelChanged = null;
			OnMuteChanged = null;
			OnManualChanged = null;
			OnNomGainEnabledChanged = null;
			OnOffAttenuationChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(CrosspointOnFeedback, AttributeCode.eCommand.Get, CROSSPOINT_ON_ATTRIBUTE, null, Index);
			RequestAttribute(DirectOutputFeedback, AttributeCode.eCommand.Get, DIRECT_OUTPUT_ATTRIBUTE, null, Index);
			RequestAttribute(GateHoldTimeFeedback, AttributeCode.eCommand.Get, GATE_HOLD_TIME_ATTRIBUTE, null, Index);
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MaxLevelFeedback, AttributeCode.eCommand.Get, MAX_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MinLevelFeedback, AttributeCode.eCommand.Get, MIN_LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(ManualFeedback, AttributeCode.eCommand.Get, MANUAL_ATTRIBUTE, null, Index);
			RequestAttribute(NomGainEnabledFeedback, AttributeCode.eCommand.Get, NOM_GAIN_ENABLED_ATTRIBUTE, null, Index);
			RequestAttribute(OffAttenuationFeedback, AttributeCode.eCommand.Get, OFF_ATTENUATION_ATTRIBUTE, null, Index);
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
		public void SetDirectOutput(eDirectOutput directOutput)
		{
			Value value = Value.FromObject(directOutput, s_DirectOutputModes);
			RequestAttribute(DirectOutputFeedback, AttributeCode.eCommand.Set, DIRECT_OUTPUT_ATTRIBUTE, value, Index);
		}

		[PublicAPI]
		public void SetGateHoldTime(float gateHoldTime)
		{
			RequestAttribute(GateHoldTimeFeedback, AttributeCode.eCommand.Set, GATE_HOLD_TIME_ATTRIBUTE, new Value(gateHoldTime), Index);
		}

		[PublicAPI]
		public void IncrementGateHoldTime()
		{
			RequestAttribute(GateHoldTimeFeedback, AttributeCode.eCommand.Increment, GATE_HOLD_TIME_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementGateHoldTime()
		{
			RequestAttribute(GateHoldTimeFeedback, AttributeCode.eCommand.Decrement, GATE_HOLD_TIME_ATTRIBUTE, null, Index);
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
		public void SetManual(bool manual)
		{
			RequestAttribute(ManualFeedback, AttributeCode.eCommand.Set, MANUAL_ATTRIBUTE, new Value(manual), Index);
		}

		[PublicAPI]
		public void ToggleManual()
		{
			RequestAttribute(ManualFeedback, AttributeCode.eCommand.Toggle, MANUAL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetNomGainEnabled(bool enabled)
		{
			RequestAttribute(NomGainEnabledFeedback, AttributeCode.eCommand.Set, NOM_GAIN_ENABLED_ATTRIBUTE, new Value(enabled), Index);
		}

		[PublicAPI]
		public void ToggleNomGainEnabled()
		{
			RequestAttribute(NomGainEnabledFeedback, AttributeCode.eCommand.Toggle, NOM_GAIN_ENABLED_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetOffAttenuation(float offAttenuation)
		{
			RequestAttribute(OffAttenuationFeedback, AttributeCode.eCommand.Set, OFF_ATTENUATION_ATTRIBUTE, new Value(offAttenuation), Index);
		}

		[PublicAPI]
		public void IncrementOffAttenuation()
		{
			RequestAttribute(OffAttenuationFeedback, AttributeCode.eCommand.Increment, OFF_ATTENUATION_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementOffAttenuation()
		{
			RequestAttribute(OffAttenuationFeedback, AttributeCode.eCommand.Decrement, OFF_ATTENUATION_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void CrosspointOnFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				CrosspointOn = innerValue.BoolValue;
		}

		private void DirectOutputFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				DirectOutput = (value["value"] as Value).GetObjectValue(s_DirectOutputModes);
		}

		private void GateHoldTimeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				GateHoldTime = innerValue.FloatValue;
		}

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				Label = innerValue.StringValue;
		}

		private void LevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				Level = innerValue.FloatValue;
		}

		private void MaxLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				MaxLevel = innerValue.FloatValue;
		}

		private void MinLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				MinLevel = innerValue.FloatValue;
		}

		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				Mute = innerValue.BoolValue;
		}

		private void ManualFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				Manual = innerValue.BoolValue;
		}

		private void NomGainEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				NomGainEnabled = innerValue.BoolValue;
		}

		private void OffAttenuationFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				OffAttenuation = innerValue.FloatValue;
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

			addRow("Crosspoint On", CrosspointOn);
			addRow("Direct Output", DirectOutput);
			addRow("Gate Hold Time", GateHoldTime);
			addRow("Label", Label);
			addRow("Level", Level);
			addRow("Min Level", MinLevel);
			addRow("Max Level", MaxLevel);
			addRow("Mute", Mute);
			addRow("Manual", Manual);
			addRow("NOM Gain Enabled", NomGainEnabled);
			addRow("Off Attenuation", OffAttenuation);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetCrosspointOn", "SetCrosspointOn <true/false>", b => SetCrosspointOn(b));
			yield return new ConsoleCommand("ToggleCrosspointOn", "", () => ToggleCrosspointOn());

			string setDirectOutputHelp =
				string.Format("SetDirectOutput <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eDirectOutput>()));
			yield return new GenericConsoleCommand<eDirectOutput>("SetDirectOutput", setDirectOutputHelp, e => SetDirectOutput(e));

			yield return new GenericConsoleCommand<float>("SetGateHoldTime", "SetGateHoldTime <MILLISECONDS>", f => SetGateHoldTime(f));
			yield return new ConsoleCommand("IncrementGateHoldTime", "", () => IncrementGateHoldTime());
			yield return new ConsoleCommand("DecrementGateHoldTime", "", () => DecrementGateHoldTime());

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

			yield return new GenericConsoleCommand<bool>("SetManual", "SetManual <true/false>", b => SetManual(b));
			yield return new ConsoleCommand("ToggleManual", "", () => ToggleManual());

			yield return new GenericConsoleCommand<bool>("SetNomGainEnabled", "SetNomGainEnabled <true/false>", b => SetNomGainEnabled(b));
			yield return new ConsoleCommand("ToggleNomGainEnabled", "", () => ToggleNomGainEnabled());

			yield return new GenericConsoleCommand<float>("SetOffAttenuation", "SetOffAttenuation <OFF-ATTENUATION>", f => SetOffAttenuation(f));
			yield return new ConsoleCommand("IncrementOffAttenuation", "", () => IncrementOffAttenuation());
			yield return new ConsoleCommand("DecrementOffAttenuation", "", () => DecrementOffAttenuation());
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
