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

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.Aec
{
	public sealed class AecProcessingChannel : AbstractAttributeChild<AecProcessingBlock>, IVolumeAttributeInterface
	{
		public enum eConferencingMode
		{
			Test,
			Telephone,
			Voip,
			Video,
			ConfModeCustom
		}

		public enum eNoiseReduction
		{
			Off,
			Low,
			Med,
			High,
			NoiseRedModeCustom
		}

		public enum ePreEmphasisSlope
		{
			Slope0,
			Slope1,
			Slope2,
			Slope3
		}

		private static readonly Dictionary<string, eConferencingMode> s_ConferencingModes
			= new Dictionary<string, eConferencingMode>(StringComparer.InvariantCultureIgnoreCase)
		{
			{"TEST", eConferencingMode.Test},
			{"TELEPHONE", eConferencingMode.Telephone},
			{"VOIP", eConferencingMode.Voip},
			{"VIDEO", eConferencingMode.Video},
			{"CONF_MODE_CUSTOM", eConferencingMode.ConfModeCustom}
		};

		private static readonly Dictionary<string, eNoiseReduction> s_NoiseReductionEnums
			= new Dictionary<string, eNoiseReduction>(StringComparer.InvariantCultureIgnoreCase)
		{
			{"OFF", eNoiseReduction.Off},
			{"LOW", eNoiseReduction.Low},
			{"MED", eNoiseReduction.Med},
			{"HIGH", eNoiseReduction.High},
			{"NOISE_RED_MODE_CUSTOM", eNoiseReduction.NoiseRedModeCustom}
		};

		private static readonly Dictionary<string, ePreEmphasisSlope> s_PreEmphasisSlopeEnums
			= new Dictionary<string, ePreEmphasisSlope>(StringComparer.InvariantCultureIgnoreCase)
		{
			{"Slope_0", ePreEmphasisSlope.Slope0},
			{"Slope_1", ePreEmphasisSlope.Slope1},
			{"Slope_2", ePreEmphasisSlope.Slope2},
			{"Slope_3", ePreEmphasisSlope.Slope3}
		};

		private const string AEC_ENABLED_ATTRIBUTE = "aecEnable";
		private const string AEC_RESET_ATTRIBUTE = "aecReset";
		private const string AGC_BYPASS_ATTRIBUTE = "agcBypass";
		private const string CONFERENCING_MODE_ATTRIBUTE = "confMode";
		private const string HOLD_TIME_ATTRIBUTE = "holdTime";
		private const string HPF_BYPASS_ATTRIBUTE = "hpfBypass";
		private const string HPF_CENTER_FREQ_ATTRIBUTE = "hpfCutoff";
		private const string INVERT_ATTRIBUTE = "invert";
		private const string LEVEL_ATTRIBUTE = "level";
		private const string LIMITER_ENABLED_ATTRIBUTE = "limiterEnable";
		private const string MAX_ATTENUATION_ATTRIBUTE = "maxAttenuation";
		private const string MAX_GAIN_ATTRIBUTE = "maxGain";
		private const string MAX_GAIN_ADJ_RATE_ATTRIBUTE = "maxGainAdjRate";
		private const string MIN_SNR_ATTRIBUTE = "minSnr";
		private const string MIN_THRESHOLD_ATTRIBUTE = "minThreshold";
		private const string MUTE_ATTRIBUTE = "mute";
		private const string NOISE_REDUCTION_ATTRIBUTE = "nrdMode";
		private const string PRE_EMPHASIS_SLOPE_ATTRIBUTE = "preEmphasisSlope";
		private const string SPEECH_MODE_ATTRIBUTE = "speechMode";
		private const string TARGET_LEVEL_ATTRIBUTE = "targetLevel";

		public delegate void ConferencingModeCallback(AecProcessingChannel sender, eConferencingMode mode);

		public delegate void NoiseReductionCallback(AecProcessingChannel sender, eNoiseReduction noiseReduction);

		public delegate void PreEmphasisSlopeCallback(AecProcessingChannel sender, ePreEmphasisSlope preEmphasisSlope);

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnAecEnabledChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnResetAecChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnBypassAgcChanged;

		[PublicAPI]
		public event ConferencingModeCallback OnConferencingModeChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnHoldTimeChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnHpfBypassChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnHpfCenterFreqChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnInvertChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnLimiterEnabledChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxAttenuationChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxGainChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxGainAdjRateChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinSnrChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinThresholdChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged;

		[PublicAPI]
		public event NoiseReductionCallback OnNoiseReductionChanged;

		[PublicAPI]
		public event PreEmphasisSlopeCallback OnPreEmphasisSlopeChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnSpeechModeChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnTargetLevelChanged;

		private bool m_AecEnabled;
		private bool m_ResetAec;
		private bool m_BypassAgc;
		private eConferencingMode m_ConferencingMode;
		private int m_HoldTime;
		private bool m_HpfBypass;
		private float m_HpfCenterFreq;
		private bool m_Invert;
		private float m_Level;
		private bool m_LimiterEnabled;
		private float m_MaxAttenuation;
		private float m_MaxGain;
		private float m_MaxGainAdjRate;
		private float m_MinSnr;
		private float m_MinThreshold;
		private bool m_Mute;
		private eNoiseReduction m_NoiseReduction;
		private ePreEmphasisSlope m_PreEmphasisSlope;
		private bool m_SpeechMode;
		private float m_TargetLevel;

		#region Properties

		[PublicAPI]
		public bool AecEnabled
		{
			get { return m_AecEnabled; }
			private set
			{
				if (value == m_AecEnabled)
					return;

				m_AecEnabled = value;

				OnAecEnabledChanged.Raise(this, new BoolEventArgs(m_AecEnabled));
			}
		}

		[PublicAPI]
		public bool ResetAec
		{
			get { return m_ResetAec; }
			private set
			{
				if (value == m_ResetAec)
					return;

				m_ResetAec = value;

				OnResetAecChanged.Raise(this, new BoolEventArgs(m_ResetAec));
			}
		}

		[PublicAPI]
		public bool BypassAgc
		{
			get { return m_BypassAgc; }
			private set
			{
				if (value == m_BypassAgc)
					return;

				m_BypassAgc = value;

				OnBypassAgcChanged.Raise(this, new BoolEventArgs(m_BypassAgc));
			}
		}

		[PublicAPI]
		public eConferencingMode ConferencingMode
		{
			get { return m_ConferencingMode; }
			private set
			{
				if (value == m_ConferencingMode)
					return;

				m_ConferencingMode = value;

				ConferencingModeCallback handler = OnConferencingModeChanged;
				if (handler != null)
					handler(this, m_ConferencingMode);
			}
		}

		[PublicAPI]
		public int HoldTime
		{
			get { return m_HoldTime; }
			private set
			{
				if (value == m_HoldTime)
					return;

				m_HoldTime = value;

				OnHoldTimeChanged.Raise(this, new IntEventArgs(m_HoldTime));
			}
		}

		[PublicAPI]
		public bool HpfBypass
		{
			get { return m_HpfBypass; }
			private set
			{
				if (value == m_HpfBypass)
					return;

				m_HpfBypass = value;

				OnHpfBypassChanged.Raise(this, new BoolEventArgs(m_HpfBypass));
			}
		}

		[PublicAPI]
		public float HpfCenterFreq
		{
			get { return m_HpfCenterFreq; }
			private set
			{
				if (value == m_HpfCenterFreq)
					return;

				m_HpfCenterFreq = value;

				OnHpfCenterFreqChanged.Raise(this, new FloatEventArgs(m_HpfCenterFreq));
			}
		}

		[PublicAPI]
		public bool Invert
		{
			get { return m_Invert; }
			private set
			{
				if (value == m_Invert)
					return;

				m_Invert = value;

				OnInvertChanged.Raise(this, new BoolEventArgs(m_Invert));
			}
		}

		[PublicAPI]
		public float Level
		{
			get { return m_Level; }
			private set
			{
				if (value == m_Level)
					return;

				m_Level = value;

				OnLevelChanged.Raise(this, new FloatEventArgs(m_Level));
			}
		}

		[PublicAPI]
		public bool LimiterEnabled
		{
			get { return m_LimiterEnabled; }
			private set
			{
				if (value == m_LimiterEnabled)
					return;

				m_LimiterEnabled = value;

				OnLimiterEnabledChanged.Raise(this, new BoolEventArgs(m_LimiterEnabled));
			}
		}

		[PublicAPI]
		public float MaxAttenuation
		{
			get { return m_MaxAttenuation; }
			private set
			{
				if (value == m_MaxAttenuation)
					return;

				m_MaxAttenuation = value;

				OnMaxAttenuationChanged.Raise(this, new FloatEventArgs(m_MaxAttenuation));
			}
		}

		[PublicAPI]
		public float MaxGain
		{
			get { return m_MaxGain; }
			private set
			{
				if (value == m_MaxGain)
					return;

				m_MaxGain = value;

				OnMaxGainChanged.Raise(this, new FloatEventArgs(m_MaxGain));
			}
		}

		[PublicAPI]
		public float MaxGainAdjRate
		{
			get { return m_MaxGainAdjRate; }
			private set
			{
				if (value == m_MaxGainAdjRate)
					return;

				m_MaxGainAdjRate = value;

				OnMaxGainAdjRateChanged.Raise(this, new FloatEventArgs(m_MaxGainAdjRate));
			}
		}

		[PublicAPI]
		public float MinSnr
		{
			get { return m_MinSnr; }
			private set
			{
				if (value == m_MinSnr)
					return;

				m_MinSnr = value;

				OnMinSnrChanged.Raise(this, new FloatEventArgs(m_MinSnr));
			}
		}

		[PublicAPI]
		public float MinThreshold
		{
			get { return m_MinThreshold; }
			private set
			{
				if (value == m_MinThreshold)
					return;

				m_MinThreshold = value;

				OnMinThresholdChanged.Raise(this, new FloatEventArgs(m_MinThreshold));
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
		public eNoiseReduction NoiseReduction
		{
			get { return m_NoiseReduction; }
			private set
			{
				if (value == m_NoiseReduction)
					return;

				m_NoiseReduction = value;

				NoiseReductionCallback handler = OnNoiseReductionChanged;
				if (handler != null)
					handler(this, m_NoiseReduction);
			}
		}

		[PublicAPI]
		public ePreEmphasisSlope PreEmphasisSlope
		{
			get { return m_PreEmphasisSlope; }
			private set
			{
				if (value == m_PreEmphasisSlope)
					return;

				m_PreEmphasisSlope = value;

				PreEmphasisSlopeCallback handler = OnPreEmphasisSlopeChanged;
				if (handler != null)
					handler(this, m_PreEmphasisSlope);
			}
		}

		[PublicAPI]
		public bool SpeechMode
		{
			get { return m_SpeechMode; }
			private set
			{
				if (value == m_SpeechMode)
					return;

				m_SpeechMode = value;

				OnSpeechModeChanged.Raise(this, new BoolEventArgs(m_SpeechMode));
			}
		}

		[PublicAPI]
		public float TargetLevel
		{
			get { return m_TargetLevel; }
			private set
			{
				if (value == m_TargetLevel)
					return;

				m_TargetLevel = value;

				OnTargetLevelChanged.Raise(this, new FloatEventArgs(m_TargetLevel));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public AecProcessingChannel(AecProcessingBlock parent, int index)
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
			OnAecEnabledChanged = null;
			OnResetAecChanged = null;
			OnBypassAgcChanged = null;
			OnConferencingModeChanged = null;
			OnHoldTimeChanged = null;
			OnHpfBypassChanged = null;
			OnHpfCenterFreqChanged = null;
			OnInvertChanged = null;
			OnLevelChanged = null;
			OnLimiterEnabledChanged = null;
			OnMaxAttenuationChanged = null;
			OnMaxGainChanged = null;
			OnMaxGainAdjRateChanged = null;
			OnMinSnrChanged = null;
			OnMinThresholdChanged = null;
			OnMuteChanged = null;
			OnNoiseReductionChanged = null;
			OnPreEmphasisSlopeChanged = null;
			OnSpeechModeChanged = null;
			OnTargetLevelChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Unsubscribe, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Unsubscribe, MUTE_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(AecEnabledFeedback, AttributeCode.eCommand.Get, AEC_ENABLED_ATTRIBUTE, null, Index);
			RequestAttribute(AecResetFeedback, AttributeCode.eCommand.Get, AEC_RESET_ATTRIBUTE, null, Index);
			RequestAttribute(AgcBypassFeedback, AttributeCode.eCommand.Get, AGC_BYPASS_ATTRIBUTE, null, Index);
			RequestAttribute(ConferencingModeFeedback, AttributeCode.eCommand.Get, CONFERENCING_MODE_ATTRIBUTE, null, Index);
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Get, HOLD_TIME_ATTRIBUTE, null, Index);
			RequestAttribute(HpfBypassFeedback, AttributeCode.eCommand.Get, HPF_BYPASS_ATTRIBUTE, null, Index);
			RequestAttribute(HpfCenterFreqFeedback, AttributeCode.eCommand.Get, HPF_CENTER_FREQ_ATTRIBUTE, null, Index);
			RequestAttribute(InvertFeedback, AttributeCode.eCommand.Get, INVERT_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(LimiterEnabledFeedback, AttributeCode.eCommand.Get, LIMITER_ENABLED_ATTRIBUTE, null, Index);
			RequestAttribute(MaxAttenuationFeedback, AttributeCode.eCommand.Get, MAX_ATTENUATION_ATTRIBUTE, null, Index);
			RequestAttribute(MaxGainFeedback, AttributeCode.eCommand.Get, MAX_GAIN_ATTRIBUTE, null, Index);
			RequestAttribute(MaxGainAdjRateFeedback, AttributeCode.eCommand.Get, MAX_GAIN_ADJ_RATE_ATTRIBUTE, null, Index);
			RequestAttribute(MinSnrFeedback, AttributeCode.eCommand.Get, MIN_SNR_ATTRIBUTE, null, Index);
			RequestAttribute(MinThresholdFeedback, AttributeCode.eCommand.Get, MIN_THRESHOLD_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(NoiseReductionFeedback, AttributeCode.eCommand.Get, NOISE_REDUCTION_ATTRIBUTE, null, Index);
			RequestAttribute(PreEmphasisSlopeFeedback, AttributeCode.eCommand.Get, PRE_EMPHASIS_SLOPE_ATTRIBUTE, null, Index);
			RequestAttribute(SpeechModeFeedback, AttributeCode.eCommand.Get, SPEECH_MODE_ATTRIBUTE, null, Index);
			RequestAttribute(TargetLevelFeedback, AttributeCode.eCommand.Get, TARGET_LEVEL_ATTRIBUTE, null, Index);

			// Subscribe
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Subscribe, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Subscribe, MUTE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetAecEnabled(bool enabled)
		{
			RequestAttribute(AecEnabledFeedback, AttributeCode.eCommand.Set, AEC_ENABLED_ATTRIBUTE, new Value(enabled), Index);
		}

		[PublicAPI]
		public void ToggleAecEnabled()
		{
			RequestAttribute(AecEnabledFeedback, AttributeCode.eCommand.Toggle, AEC_ENABLED_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetResetAec(bool reset)
		{
			RequestAttribute(AecResetFeedback, AttributeCode.eCommand.Set, AEC_RESET_ATTRIBUTE, new Value(reset), Index);
		}

		[PublicAPI]
		public void ToggleResetAec()
		{
			RequestAttribute(AecResetFeedback, AttributeCode.eCommand.Toggle, AEC_RESET_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetBypassAgc(bool bypass)
		{
			RequestAttribute(AgcBypassFeedback, AttributeCode.eCommand.Set, AGC_BYPASS_ATTRIBUTE, new Value(bypass), Index);
		}

		[PublicAPI]
		public void ToggleBypassAgc()
		{
			RequestAttribute(AgcBypassFeedback, AttributeCode.eCommand.Toggle, AGC_BYPASS_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetConferencingMode(eConferencingMode mode)
		{
			Value value = Value.FromObject(mode, s_ConferencingModes);
			RequestAttribute(ConferencingModeFeedback, AttributeCode.eCommand.Set, CONFERENCING_MODE_ATTRIBUTE, value, Index);
		}

		[PublicAPI]
		public void SetHoldTime(int seconds)
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Set, HOLD_TIME_ATTRIBUTE, new Value(seconds), Index);
		}

		[PublicAPI]
		public void IncrementHoldTime()
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Increment, HOLD_TIME_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementHoldTime()
		{
			RequestAttribute(HoldTimeFeedback, AttributeCode.eCommand.Decrement, HOLD_TIME_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetHpfBypass(bool bypass)
		{
			RequestAttribute(HpfBypassFeedback, AttributeCode.eCommand.Set, HPF_BYPASS_ATTRIBUTE, new Value(bypass), Index);
		}

		[PublicAPI]
		public void ToggleHpfBypass()
		{
			RequestAttribute(HpfBypassFeedback, AttributeCode.eCommand.Toggle, HPF_BYPASS_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetHpfCenterFreq(float cutoff)
		{
			RequestAttribute(HpfCenterFreqFeedback, AttributeCode.eCommand.Set, HPF_CENTER_FREQ_ATTRIBUTE, new Value(cutoff), Index);
		}

		[PublicAPI]
		public void IncrementHpfCenterFreq()
		{
			RequestAttribute(HpfCenterFreqFeedback, AttributeCode.eCommand.Increment, HPF_CENTER_FREQ_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementHpfCenterFreq()
		{
			RequestAttribute(HpfCenterFreqFeedback, AttributeCode.eCommand.Decrement, HPF_CENTER_FREQ_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetInvert(bool invert)
		{
			RequestAttribute(InvertFeedback, AttributeCode.eCommand.Set, INVERT_ATTRIBUTE, new Value(invert), Index);
		}

		[PublicAPI]
		public void ToggleInvert()
		{
			RequestAttribute(InvertFeedback, AttributeCode.eCommand.Toggle, INVERT_ATTRIBUTE, null, Index);
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
		public void SetLimiterEnabled(bool enabled)
		{
			RequestAttribute(LimiterEnabledFeedback, AttributeCode.eCommand.Set, LIMITER_ENABLED_ATTRIBUTE, new Value(enabled), Index);
		}

		[PublicAPI]
		public void ToggleLimiterEnabled()
		{
			RequestAttribute(LimiterEnabledFeedback, AttributeCode.eCommand.Toggle, LIMITER_ENABLED_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMaxAttenuation(float maxAttenuation)
		{
			RequestAttribute(MaxAttenuationFeedback, AttributeCode.eCommand.Set, MAX_ATTENUATION_ATTRIBUTE, new Value(maxAttenuation), Index);
		}

		[PublicAPI]
		public void IncrementMaxAttenuation()
		{
			RequestAttribute(MaxAttenuationFeedback, AttributeCode.eCommand.Increment, MAX_ATTENUATION_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMaxAttenuation()
		{
			RequestAttribute(MaxAttenuationFeedback, AttributeCode.eCommand.Decrement, MAX_ATTENUATION_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMaxGain(float maxGain)
		{
			RequestAttribute(MaxGainFeedback, AttributeCode.eCommand.Set, MAX_GAIN_ATTRIBUTE, new Value(maxGain), Index);
		}

		[PublicAPI]
		public void IncrementMaxGain()
		{
			RequestAttribute(MaxGainFeedback, AttributeCode.eCommand.Increment, MAX_GAIN_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMaxGain()
		{
			RequestAttribute(MaxGainFeedback, AttributeCode.eCommand.Decrement, MAX_GAIN_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMaxGainAdjRate(float maxGainAdjRate)
		{
			RequestAttribute(MaxGainAdjRateFeedback, AttributeCode.eCommand.Set, MAX_GAIN_ADJ_RATE_ATTRIBUTE, new Value(maxGainAdjRate), Index);
		}

		[PublicAPI]
		public void IncrementMaxGainAdjRate()
		{
			RequestAttribute(MaxGainAdjRateFeedback, AttributeCode.eCommand.Increment, MAX_GAIN_ADJ_RATE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMaxGainAdjRate()
		{
			RequestAttribute(MaxGainAdjRateFeedback, AttributeCode.eCommand.Decrement, MAX_GAIN_ADJ_RATE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMinSnr(float minSnr)
		{
			RequestAttribute(MinSnrFeedback, AttributeCode.eCommand.Set, MIN_SNR_ATTRIBUTE, new Value(minSnr), Index);
		}

		[PublicAPI]
		public void IncrementMinSnr()
		{
			RequestAttribute(MinSnrFeedback, AttributeCode.eCommand.Increment, MIN_SNR_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMinSnr()
		{
			RequestAttribute(MinSnrFeedback, AttributeCode.eCommand.Decrement, MIN_SNR_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMinThreshold(float minThreshold)
		{
			RequestAttribute(MinThresholdFeedback, AttributeCode.eCommand.Set, MIN_THRESHOLD_ATTRIBUTE, new Value(minThreshold), Index);
		}

		[PublicAPI]
		public void IncrementMinThreshold()
		{
			RequestAttribute(MinThresholdFeedback, AttributeCode.eCommand.Increment, MIN_THRESHOLD_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementMinThreshold()
		{
			RequestAttribute(MinThresholdFeedback, AttributeCode.eCommand.Decrement, MIN_THRESHOLD_ATTRIBUTE, null, Index);
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
		public void SetNoiseReduction(eNoiseReduction mode)
		{
			Value value = Value.FromObject(mode, s_NoiseReductionEnums);
			RequestAttribute(NoiseReductionFeedback, AttributeCode.eCommand.Set, NOISE_REDUCTION_ATTRIBUTE, value, Index);
		}

		[PublicAPI]
		public void SetPreEmphasisSlope(ePreEmphasisSlope mode)
		{
			Value value = Value.FromObject(mode, s_PreEmphasisSlopeEnums);
			RequestAttribute(PreEmphasisSlopeFeedback, AttributeCode.eCommand.Set, PRE_EMPHASIS_SLOPE_ATTRIBUTE, value, Index);
		}

		[PublicAPI]
		public void SetSpeechMode(bool mode)
		{
			RequestAttribute(SpeechModeFeedback, AttributeCode.eCommand.Set, SPEECH_MODE_ATTRIBUTE, new Value(mode), Index);
		}

		[PublicAPI]
		public void ToggleSpeechMode()
		{
			RequestAttribute(SpeechModeFeedback, AttributeCode.eCommand.Toggle, SPEECH_MODE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetTargetLevel(float targetLevel)
		{
			RequestAttribute(TargetLevelFeedback, AttributeCode.eCommand.Set, TARGET_LEVEL_ATTRIBUTE, new Value(targetLevel), Index);
		}

		[PublicAPI]
		public void IncrementTargetLevel()
		{
			RequestAttribute(TargetLevelFeedback, AttributeCode.eCommand.Increment, TARGET_LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void DecrementTargetLevel()
		{
			RequestAttribute(TargetLevelFeedback, AttributeCode.eCommand.Decrement, TARGET_LEVEL_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void LevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Level = (value["value"] as Value).FloatValue;
		}

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Mute = (value["value"] as Value).BoolValue;
		}

		private void AecEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			AecEnabled = (value["value"] as Value).BoolValue;
		}

		private void AecResetFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ResetAec = (value["value"] as Value).BoolValue;
		}

		private void AgcBypassFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			BypassAgc = (value["value"] as Value).BoolValue;
		}

		private void ConferencingModeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ConferencingMode = (value["value"] as Value).GetObjectValue(s_ConferencingModes);
		}

		private void HoldTimeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			HoldTime = (value["value"] as Value).IntValue;
		}

		private void HpfBypassFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			HpfBypass = (value["value"] as Value).BoolValue;
		}

		private void HpfCenterFreqFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			HpfCenterFreq = (value["value"] as Value).FloatValue;
		}

		private void InvertFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Invert = (value["value"] as Value).BoolValue;
		}

		private void LimiterEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LimiterEnabled = (value["value"] as Value).BoolValue;
		}

		private void MaxAttenuationFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MaxAttenuation = (value["value"] as Value).FloatValue;
		}

		private void MaxGainFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MaxGain = (value["value"] as Value).FloatValue;
		}

		private void MaxGainAdjRateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MaxGainAdjRate = (value["value"] as Value).FloatValue;
		}

		private void MinSnrFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MinSnr = (value["value"] as Value).FloatValue;
		}

		private void MinThresholdFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MinThreshold = (value["value"] as Value).FloatValue;
		}

		private void NoiseReductionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			NoiseReduction = (value["value"] as Value).GetObjectValue(s_NoiseReductionEnums);
		}

		private void PreEmphasisSlopeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			PreEmphasisSlope = (value["value"] as Value).GetObjectValue(s_PreEmphasisSlopeEnums);
		}

		private void SpeechModeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			SpeechMode = (value["value"] as Value).BoolValue;
		}

		private void TargetLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			TargetLevel = (value["value"] as Value).FloatValue;
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

			addRow("AEC Enabled", AecEnabled);
			addRow("Reset AEC", ResetAec);
			addRow("Bypass AGC", BypassAgc);
			addRow("Conferencing Mode", ConferencingMode);
			addRow("Hold Time", HoldTime);
			addRow("HPF Bypass", HpfBypass);
			addRow("HPF Center Freq", HpfCenterFreq);
			addRow("Invert", Invert);
			addRow("Level", Level);
			addRow("Limiter Enabled", LimiterEnabled);
			addRow("Max Attenuation", MaxAttenuation);
			addRow("Max Gain", MaxGain);
			addRow("Max Gain Adj Rate", MaxGainAdjRate);
			addRow("Min SNR", MinSnr);
			addRow("Min Threshold", MinThreshold);
			addRow("Mute", Mute);
			addRow("Noise Reduction", m_NoiseReduction);
			addRow("Pre-Emphasis Slope", m_PreEmphasisSlope);
			addRow("Speech Mode", m_SpeechMode);
			addRow("Target Level", m_TargetLevel);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetAecEnabled", "SetAecEnabled <true/false>", b => SetAecEnabled(b));
			yield return new ConsoleCommand("ToggleAecEnabled", "", () => ToggleAecEnabled());
			yield return new GenericConsoleCommand<bool>("SetResetAec", "SetResetAec <true/false>", b => SetResetAec(b));
			yield return new ConsoleCommand("ToggleResetAec", "", () => ToggleResetAec());
			yield return new GenericConsoleCommand<bool>("SetBypassAgc", "SetBypassAgc <true/false>", b => SetBypassAgc(b));
			yield return new ConsoleCommand("ToggleBypassAgc", "", () => ToggleBypassAgc());

			string setConferencingModeHelp =
				string.Format("SetConferencingMode <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eConferencingMode>()));
			yield return new GenericConsoleCommand<eConferencingMode>("SetConferencingMode", setConferencingModeHelp, e => SetConferencingMode(e));

			yield return new GenericConsoleCommand<int>("SetHoldTime", "SetHoldTime <SECONDS>", i => SetHoldTime(i));
			yield return new ConsoleCommand("IncrementHoldTime", "", () => IncrementHoldTime());
			yield return new ConsoleCommand("DecrementHoldTime", "", () => DecrementHoldTime());

			yield return new GenericConsoleCommand<bool>("SetHpfBypass", "SetHpfBypass <true/false>", b => SetHpfBypass(b));
			yield return new ConsoleCommand("ToggleHpfBypass", "", () => ToggleHpfBypass());

			yield return new GenericConsoleCommand<float>("SetHpfCenterFreq", "SetHpfCenterFreq <CUTOFF>", f => SetHpfCenterFreq(f));
			yield return new ConsoleCommand("IncrementHpfCenterFreq", "", () => IncrementHpfCenterFreq());
			yield return new ConsoleCommand("DecrementHpfCenterFreq", "", () => DecrementHpfCenterFreq());

			yield return new GenericConsoleCommand<bool>("SetInvert", "SetInvert <true/false>", b => SetInvert(b));
			yield return new ConsoleCommand("ToggleInvert", "", () => ToggleInvert());

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <LEVEL>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "", () => DecrementLevel());

			yield return new GenericConsoleCommand<float>("SetTargetLevel", "SetTargetLevel <Target Level>", f => SetTargetLevel(f));
			yield return new ConsoleCommand("IncrementTargetLevel", "", () => IncrementTargetLevel());
			yield return new ConsoleCommand("DecrementTargetLevel", "", () => DecrementTargetLevel());

			yield return new GenericConsoleCommand<bool>("SetLimiterEnabled", "SetLimiterEnabled <true/false>", b => SetLimiterEnabled(b));
			yield return new ConsoleCommand("ToggleLimiterEnabled", "", () => ToggleLimiterEnabled());

			yield return new GenericConsoleCommand<float>("SetMaxAttenuation", "SetMaxAttenuation <MAX ATTENUATION>", f => SetMaxAttenuation(f));
			yield return new ConsoleCommand("IncrementMaxAttenuation", "", () => IncrementMaxAttenuation());
			yield return new ConsoleCommand("DecrementMaxAttenuation", "", () => DecrementMaxAttenuation());

			yield return new GenericConsoleCommand<float>("SetMaxGain", "SetMaxGain <MAX GAIN>", f => SetMaxGain(f));
			yield return new ConsoleCommand("IncrementMaxGain", "", () => IncrementMaxGain());
			yield return new ConsoleCommand("DecrementMaxGain", "", () => DecrementMaxGain());

			yield return new GenericConsoleCommand<float>("SetMaxGainAdjRate", "SetMaxGainAdjRate <MAX GAIN ADJ RATE>", f => SetMaxGainAdjRate(f));
			yield return new ConsoleCommand("IncrementMaxGainAdjRate", "", () => IncrementMaxGainAdjRate());
			yield return new ConsoleCommand("DecrementMaxGainAdjRate", "", () => DecrementMaxGainAdjRate());

			yield return new GenericConsoleCommand<float>("SetMinSnr", "SetMinSnr <MAX SNR RATE>", f => SetMinSnr(f));
			yield return new ConsoleCommand("IncrementMinSnr", "", () => IncrementMinSnr());
			yield return new ConsoleCommand("DecrementMinSnr", "", () => DecrementMinSnr());

			yield return new GenericConsoleCommand<float>("SetMinThreshold", "SetMinThreshold <MIN THRESHOLD>", f => SetMinThreshold(f));
			yield return new ConsoleCommand("IncrementMinThreshold", "", () => IncrementMinThreshold());
			yield return new ConsoleCommand("DecrementMinThreshold", "", () => DecrementMinThreshold());

			yield return new GenericConsoleCommand<bool>("SetMute", "SetMute <true/false>", b => SetMute(b));
			yield return new ConsoleCommand("ToggleMute", "", () => ToggleMute());

			string setNoiseReductionHelp =
				string.Format("SetNoiseReduction <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eNoiseReduction>()));
			yield return new GenericConsoleCommand<eNoiseReduction>("SetNoiseReduction", setNoiseReductionHelp, e => SetNoiseReduction(e));

			string setPreEmphasisSlopeHelp =
				string.Format("SetPreEmphasisSlope <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<ePreEmphasisSlope>()));
			yield return new GenericConsoleCommand<ePreEmphasisSlope>("SetPreEmphasisSlope", setPreEmphasisSlopeHelp, e => SetPreEmphasisSlope(e));

			yield return new GenericConsoleCommand<bool>("SetSpeechMode", "SetSpeechMode <true/false>", b => SetSpeechMode(b));
			yield return new ConsoleCommand("ToggleSpeechMode", "", () => ToggleSpeechMode());
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