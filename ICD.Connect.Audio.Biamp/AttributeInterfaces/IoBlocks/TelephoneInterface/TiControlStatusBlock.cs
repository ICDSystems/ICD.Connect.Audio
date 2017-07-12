using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface
{
	public sealed class TiControlStatusBlock : AbstractIoBlock
	{
		public enum eTiCallState
		{
			Init,
			Dialing,
			RingBack,
			BusyTone,
			ErrorTone,
			Connected,
			ConnectedMuted,
			Ringing,
			Dropped,
			Idle,
			Fault
		}

		private static readonly Dictionary<string, eTiCallState> s_CallStateSerials =
			new Dictionary<string, eTiCallState>(StringComparer.InvariantCultureIgnoreCase)
			{
				{"TI_CALL_STATE_IDLE", eTiCallState.Idle},
				{"TI_CALL_STATE_DIALING", eTiCallState.Dialing},
				{"TI_CALL_STATE_RINGBACK", eTiCallState.RingBack},
				{"TI_CALL_STATE_BUSY_TONE", eTiCallState.BusyTone},
				{"TI_CALL_STATE_ERROR_TONE", eTiCallState.ErrorTone},
				{"TI_CALL_STATE_CONNECTED", eTiCallState.Connected},
				{"TI_CALL_STATE_RINGING", eTiCallState.Ringing},
				{"TI_CALL_STATE_DROPPED", eTiCallState.Dropped},
				{"TI_CALL_STATE_INIT", eTiCallState.Init},
				{"TI_CALL_STATE_FAULT", eTiCallState.Fault},
				{"TI_CALL_STATE_CONNECTED_MUTED", eTiCallState.ConnectedMuted}
			};

		private const string REDIAL_SERVICE = "redial";
		private const string END_SERVICE = "end";
		private const string DIAL_SERVICE = "dial";
		private const string DTMF_SERVICE = "dtmf";
		private const string ANSWER_SERVICE = "answer";

		private const string AUTO_ANSWER_ATTRIBUTE = "autoAnswer";
		private const string AUTO_ANSWER_RING_COUNT_ATTRIBUTE = "autoAnswerRingCount";
		private const string AUTO_DISCONNECT_TYPE_ATTRIBUTE = "autoDisconnect";
		private const string BUSY_TONE_DETECTED_ATTRIBUTE = "busyToneDetected";
		private const string CALLER_ID_ENABLED_ATTRIBUTE = "callerIdEnable";
		private const string CALL_STATE_ATTRIBUTE = "callState";
		private const string SIMPLE_CALLER_ID_ATTRIBUTE = "cid";
		private const string FULL_CALLER_ID_ATTRIBUTE = "cidUser";
		private const string DIALING_ATTRIBUTE = "dialing";
		private const string DIAL_TONE_DETECTED_ATTRIBUTE = "dialToneDetected";
		private const string DIAL_TONE_LEVEL_ATTRIBUTE = "dialToneLevel";
		private const string LINE_FAULT_CONDITION_ATTRIBUTE = "faultCondition";
		private const string HOOK_STATE_ATTRIBUTE = "hookState";
		private const string LAST_NUMBER_DIALED_ATTRIBUTE = "lastNum";
		private const string LINE_FAULT_ATTRIBUTE = "lineFault";
		private const string LINE_INTRUSION_ATTRIBUTE = "lineIntrusion";
		private const string LINE_IN_USE_ATTRIBUTE = "lineInUse";
		private const string LINE_READY_ATTRIBUTE = "lineReady";
		private const string LINE_VOLTAGE_ATTRIBUTE = "lineVoltage";
		private const string DTMF_LOCAL_LEVEL_ATTRIBUTE = "localDtmfToneLevel";
		private const string LOOP_CURRENT_ATTRIBUTE = "loopCurrent";
		private const string RING_BACK_TONE_DETECTED_ATTRIBUTE = "ringBackToneDetected";
		private const string RINGING_ATTRIBUTE = "ringing";
		private const string USE_REDIAL_ATTRIBUTE = "useRedial";
		private const string WAIT_FOR_DIAL_TONE_ATTRIBUTE = "waitForDialTone";

		public enum eAutoAnswerRingCount
		{
			OneRing,
			TwoRings,
			ThreeRings,
			FourRings,
			FiveRings
		}

		[Flags]
		public enum eAutoDisconnecType
		{
			None = 0,
			LoopDrop = 1,
			CallProgress = 2,
			LoopDropPlusCallProgress = LoopDrop & CallProgress
		}

		public enum eLineFaultConditions
		{
			NoFault,
			OverCurrent,
			UnderVoltage,
			UnderCurrent,
			OverVoltage,
			PolarityReversal
		}

		public enum eHookState
		{
			OffHook,
// ReSharper disable once InconsistentNaming
			OnHook
		}

		private static readonly Dictionary<string, eAutoAnswerRingCount> s_AutoAnswerRingCountSerials =
			new Dictionary<string, eAutoAnswerRingCount>(StringComparer.InvariantCultureIgnoreCase)
			{
				{"AA_ONE_RING", eAutoAnswerRingCount.OneRing},
				{"AA_TWO_RINGS", eAutoAnswerRingCount.TwoRings},
				{"AA_THREE_RINGS", eAutoAnswerRingCount.ThreeRings},
				{"AA_FOUR_RINGS", eAutoAnswerRingCount.FourRings},
				{"AA_FIVE_RINGS", eAutoAnswerRingCount.FiveRings}
			};

		private static readonly Dictionary<string, eAutoDisconnecType> s_AutoDisconnectTypeSerials =
			new Dictionary<string, eAutoDisconnecType>(StringComparer.InvariantCultureIgnoreCase)
			{
				{"AD_NONE", eAutoDisconnecType.None},
				{"AD_LOOP_DROP", eAutoDisconnecType.LoopDrop},
				{"AD_CALL_PROGRESS", eAutoDisconnecType.CallProgress},
				{"AD_LOOP_DROP_PLUS_CALL_PROGRESS", eAutoDisconnecType.LoopDropPlusCallProgress},
			};

		private static readonly Dictionary<string, eLineFaultConditions> s_LineFaultConditionsSerials =
			new Dictionary<string, eLineFaultConditions>(StringComparer.InvariantCultureIgnoreCase)
			{
				{"LINE_NO_FAULT", eLineFaultConditions.NoFault},
				{"LINE_OVERCURRENT_FAULT", eLineFaultConditions.OverCurrent},
				{"LINE_UNDERVOLTAGE_FAULT", eLineFaultConditions.UnderVoltage},
				{"LINE_UNDERCURRENT_FAULT", eLineFaultConditions.UnderCurrent},
				{"LINE_OVERVOLTAGE_FAULT", eLineFaultConditions.OverVoltage},
				{"LINE_POLARITY_REVERSAL_FAULT", eLineFaultConditions.PolarityReversal}
			};

		private static readonly Dictionary<string, eHookState> s_HookStateSerials =
			new Dictionary<string, eHookState>(StringComparer.InvariantCultureIgnoreCase)
			{
				{"OFFHOOK", eHookState.OffHook},
				{"ONHOOK", eHookState.OnHook}
			};

		public delegate void AutoAnswerRingCountCallback(TiControlStatusBlock sender, eAutoAnswerRingCount ringCount);

		public delegate void AutoDisconnectTypeCallback(TiControlStatusBlock sender, eAutoDisconnecType autoDisconnectType);

		public delegate void LineFaultConditionsCallback(TiControlStatusBlock sender, eLineFaultConditions lineFaultConditions);

		public delegate void HookStateCallback(TiControlStatusBlock sender, eHookState hookState);

		public delegate void CallStateCallback(TiControlStatusBlock sender, eTiCallState callState);

		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event AutoAnswerRingCountCallback OnAutoAnswerRingCountChanged;
		public event AutoDisconnectTypeCallback OnAutoDisconnectTypeChanged;
		public event EventHandler<BoolEventArgs> OnBusyToneDetectedChanged;
		public event EventHandler<BoolEventArgs> OnCallerIdEnabledChanged;
		public event EventHandler<BoolEventArgs> OnDialingChanged;
		public event EventHandler<BoolEventArgs> OnDialToneDetectedChanged;
		public event EventHandler<FloatEventArgs> OnDialToneLevelChanged;
		public event LineFaultConditionsCallback OnLineFaultConditionChanged;
		public event HookStateCallback OnHookStateChanged;
		public event EventHandler<StringEventArgs> OnLastNumberDialedChanged;
		public event EventHandler<BoolEventArgs> OnLineFaultChanged;
		public event EventHandler<BoolEventArgs> OnLineIntrusionChanged;
		public event EventHandler<BoolEventArgs> OnLineInUseChanged;
		public event EventHandler<BoolEventArgs> OnLineReadyChanged;
		public event EventHandler<FloatEventArgs> OnLineVoltageChanged;
		public event EventHandler<FloatEventArgs> OnDtmfLocalLevelChanged;
		public event EventHandler<FloatEventArgs> OnLoopCurrentChanged;
		public event EventHandler<BoolEventArgs> OnRingBackToneDetectedChanged;
		public event EventHandler<BoolEventArgs> OnRingingChanged;
		public event EventHandler<BoolEventArgs> OnUseRedialChanged;
		public event EventHandler<BoolEventArgs> OnWaitForDialToneChanged;

		[PublicAPI]
		public event CallStateCallback OnCallStateChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnCallerNumberChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnCallerNameChanged;

		private bool m_AutoAnswer;
		private eAutoAnswerRingCount m_AutoAnswerRingCount;
		private eAutoDisconnecType m_AutoDisconnectType;
		private bool m_BusyToneDetected;
		private bool m_CallerIdEnabled;
		private bool m_Dialing;
		private bool m_DialToneDetected;
		private float m_DialToneLevel;
		private eLineFaultConditions m_LineFaultCondition;
		private eHookState m_HookState;
		private string m_LastNumberDialed;
		private bool m_LineFault;
		private bool m_LineIntrusion;
		private bool m_LineInUse;
		private bool m_LineReady;
		private float m_LineVoltage;
		private float m_DtmfLocalLevel;
		private float m_LoopCurrent;
		private bool m_RingBackToneDetected;
		private bool m_Ringing;
		private bool m_UseRedial;
		private bool m_WaitForDialTone;
		private string m_CallerNumber;
		private string m_CallerName;
		private eTiCallState m_State;

		#region Properties

		[PublicAPI]
		public bool AutoAnswer
		{
			get
			{
				return m_AutoAnswer;
			}
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		[PublicAPI]
		public eAutoAnswerRingCount AutoAnswerRingCount
		{
			get
			{
				return m_AutoAnswerRingCount;
			}
			private set
			{
				if (value == m_AutoAnswerRingCount)
					return;

				m_AutoAnswerRingCount = value;

				AutoAnswerRingCountCallback handler = OnAutoAnswerRingCountChanged;
				if (handler != null)
					handler(this, m_AutoAnswerRingCount);
			}
		}

		[PublicAPI]
		public eAutoDisconnecType AutoDisconnectType
		{
			get
			{
				return m_AutoDisconnectType;
			}
			private set
			{
				if (value == m_AutoDisconnectType)
					return;

				m_AutoDisconnectType = value;

				AutoDisconnectTypeCallback handler = OnAutoDisconnectTypeChanged;
				if (handler != null)
					handler(this, m_AutoDisconnectType);
			}
		}

		[PublicAPI]
		public bool BusyToneDetected
		{
			get
			{
				return m_BusyToneDetected;
			}
			private set
			{
				if (value == m_BusyToneDetected)
					return;

				m_BusyToneDetected = value;

				OnBusyToneDetectedChanged.Raise(this, new BoolEventArgs(m_BusyToneDetected));
			}
		}

		[PublicAPI]
		public bool CallerIdEnabled
		{
			get
			{
				return m_CallerIdEnabled;
			}
			private set
			{
				if (value == m_CallerIdEnabled)
					return;

				m_CallerIdEnabled = value;

				OnCallerIdEnabledChanged.Raise(this, new BoolEventArgs(m_CallerIdEnabled));
			}
		}

		[PublicAPI]
		public bool Dialing
		{
			get
			{
				return m_Dialing;
			}
			private set
			{
				if (value == m_Dialing)
					return;

				m_Dialing = value;

				OnDialingChanged.Raise(this, new BoolEventArgs(m_Dialing));
			}
		}

		[PublicAPI]
		public bool DialToneDetected
		{
			get
			{
				return m_DialToneDetected;
			}
			private set
			{
				if (value == m_DialToneDetected)
					return;

				m_DialToneDetected = value;

				OnDialToneDetectedChanged.Raise(this, new BoolEventArgs(m_DialToneDetected));
			}
		}

		[PublicAPI]
		public float DialToneLevel
		{
			get
			{
				return m_DialToneLevel;
			}
			private set
			{
				if (value == m_DialToneLevel)
					return;

				m_DialToneLevel = value;

				OnDialToneLevelChanged.Raise(this, new FloatEventArgs(m_DialToneLevel));
			}
		}

		[PublicAPI]
		public eLineFaultConditions LineFaultCondition
		{
			get
			{
				return m_LineFaultCondition;
			}
			private set
			{
				if (value == m_LineFaultCondition)
					return;

				m_LineFaultCondition = value;

				LineFaultConditionsCallback handler = OnLineFaultConditionChanged;
				if (handler != null)
					handler(this, m_LineFaultCondition);
			}
		}

		[PublicAPI]
		public eHookState HookState
		{
			get
			{
				return m_HookState;
			}
			private set
			{
				if (value == m_HookState)
					return;

				m_HookState = value;

				HookStateCallback handler = OnHookStateChanged;
				if (handler != null)
					handler(this, m_HookState);
			}
		}

		[PublicAPI]
		public string LastNumberDialed
		{
			get
			{
				return m_LastNumberDialed;
			}
			private set
			{
				if (value == m_LastNumberDialed)
					return;

				m_LastNumberDialed = value;

				OnLastNumberDialedChanged.Raise(this, new StringEventArgs(m_LastNumberDialed));
			}
		}

		[PublicAPI]
		public bool LineFault
		{
			get
			{
				return m_LineFault;
			}
			private set
			{
				if (value == m_LineFault)
					return;

				m_LineFault = value;

				OnLineFaultChanged.Raise(this, new BoolEventArgs(m_LineFault));
			}
		}

		[PublicAPI]
		public bool LineIntrusion
		{
			get
			{
				return m_LineIntrusion;
			}
			private set
			{
				if (value == m_LineIntrusion)
					return;

				m_LineIntrusion = value;

				OnLineIntrusionChanged.Raise(this, new BoolEventArgs(m_LineIntrusion));
			}
		}

		[PublicAPI]
		public bool LineInUse
		{
			get
			{
				return m_LineInUse;
			}
			private set
			{
				if (value == m_LineInUse)
					return;

				m_LineInUse = value;

				OnLineInUseChanged.Raise(this, new BoolEventArgs(m_LineInUse));
			}
		}

		[PublicAPI]
		public bool LineReady
		{
			get
			{
				return m_LineReady;
			}
			private set
			{
				if (value == m_LineReady)
					return;

				m_LineReady = value;

				OnLineReadyChanged.Raise(this, new BoolEventArgs(m_LineReady));
			}
		}

		[PublicAPI]
		public float LineVoltage
		{
			get
			{
				return m_LineVoltage;
			}
			private set
			{
				if (value == m_LineVoltage)
					return;

				m_LineVoltage = value;

				OnLineVoltageChanged.Raise(this, new FloatEventArgs(m_LineVoltage));
			}
		}

		[PublicAPI]
		public float DtmfLocalLevel
		{
			get
			{
				return m_DtmfLocalLevel;
			}
			private set
			{
				if (value == m_DtmfLocalLevel)
					return;

				m_DtmfLocalLevel = value;

				OnDtmfLocalLevelChanged.Raise(this, new FloatEventArgs(m_DtmfLocalLevel));
			}
		}

		[PublicAPI]
		public float LoopCurrent
		{
			get
			{
				return m_LoopCurrent;
			}
			private set
			{
				if (value == m_LoopCurrent)
					return;

				m_LoopCurrent = value;

				OnLoopCurrentChanged.Raise(this, new FloatEventArgs(m_LoopCurrent));
			}
		}

		[PublicAPI]
		public bool RingBackToneDetected
		{
			get
			{
				return m_RingBackToneDetected;
			}
			private set
			{
				if (value == m_RingBackToneDetected)
					return;

				m_RingBackToneDetected = value;

				OnRingBackToneDetectedChanged.Raise(this, new BoolEventArgs(m_RingBackToneDetected));
			}
		}

		[PublicAPI]
		public bool Ringing
		{
			get
			{
				return m_Ringing;
			}
			private set
			{
				if (value == m_Ringing)
					return;

				m_Ringing = value;

				OnRingingChanged.Raise(this, new BoolEventArgs(m_Ringing));
			}
		}

		[PublicAPI]
		public bool UseRedial
		{
			get
			{
				return m_UseRedial;
			}
			private set
			{
				if (value == m_UseRedial)
					return;

				m_UseRedial = value;

				OnUseRedialChanged.Raise(this, new BoolEventArgs(m_UseRedial));
			}
		}

		[PublicAPI]
		public bool WaitForDialTone
		{
			get { return m_WaitForDialTone; }
			private set
			{
				if (value == m_WaitForDialTone)
					return;

				m_WaitForDialTone = value;

				OnWaitForDialToneChanged.Raise(this, new BoolEventArgs(m_WaitForDialTone));
			}
		}

		[PublicAPI]
		public eTiCallState State
		{
			get { return m_State; }
			private set
			{
				if (value == m_State)
					return;
				
				m_State = value;

				CallStateCallback handler = OnCallStateChanged;
				if (handler != null)
					handler(this, m_State);
			}
		}

		[PublicAPI]
		public string CallerNumber
		{
			get { return m_CallerNumber; }
			private set
			{
				if (value == m_CallerNumber)
					return;

				m_CallerNumber = value;

				OnCallerNumberChanged.Raise(this, new StringEventArgs(m_CallerNumber));
			}
		}

		[PublicAPI]
		public string CallerName
		{
			get { return m_CallerName; }
			private set
			{
				if (value == m_CallerName)
					return;

				m_CallerName = value;

				OnCallerNameChanged.Raise(this, new StringEventArgs(m_CallerName));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public TiControlStatusBlock(BiampTesiraDevice device, string instanceTag)
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
			OnAutoAnswerChanged = null;
			OnAutoAnswerRingCountChanged = null;
			OnAutoDisconnectTypeChanged = null;
			OnBusyToneDetectedChanged = null;
			OnCallerIdEnabledChanged = null;
			OnDialingChanged = null;
			OnDialToneDetectedChanged = null;
			OnDialToneLevelChanged = null;
			OnLineFaultConditionChanged = null;
			OnHookStateChanged = null;
			OnLastNumberDialedChanged = null;
			OnLineFaultChanged = null;
			OnLineIntrusionChanged = null;
			OnLineInUseChanged = null;
			OnLineReadyChanged = null;
			OnLineVoltageChanged = null;
			OnDtmfLocalLevelChanged = null;
			OnLoopCurrentChanged = null;
			OnRingBackToneDetectedChanged = null;
			OnRingingChanged = null;
			OnUseRedialChanged = null;
			OnWaitForDialToneChanged = null;
			OnCallerNumberChanged = null;
			OnCallerNameChanged = null;
			OnCallStateChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(BusyToneDetectedFeedback, AttributeCode.eCommand.Unsubscribe, BUSY_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Unsubscribe, CALL_STATE_ATTRIBUTE, null);
			RequestAttribute(DialingFeedback, AttributeCode.eCommand.Unsubscribe, DIALING_ATTRIBUTE, null);
			RequestAttribute(DialToneDetectedFeedback, AttributeCode.eCommand.Unsubscribe, DIAL_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(LineFaultConditionFeedback, AttributeCode.eCommand.Unsubscribe, LINE_FAULT_CONDITION_ATTRIBUTE, null);
			RequestAttribute(HookStateFeedback, AttributeCode.eCommand.Unsubscribe, HOOK_STATE_ATTRIBUTE, null);
			RequestAttribute(LastNumberDialedFeedback, AttributeCode.eCommand.Unsubscribe, LAST_NUMBER_DIALED_ATTRIBUTE, null);
			RequestAttribute(LineFaultFeedback, AttributeCode.eCommand.Unsubscribe, LINE_FAULT_ATTRIBUTE, null);
			RequestAttribute(LineIntrusionFeedback, AttributeCode.eCommand.Unsubscribe, LINE_INTRUSION_ATTRIBUTE, null);
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Unsubscribe, LINE_IN_USE_ATTRIBUTE, null);
			RequestAttribute(LineReadyFeedback, AttributeCode.eCommand.Unsubscribe, LINE_READY_ATTRIBUTE, null);
			RequestAttribute(LineVoltageFeedback, AttributeCode.eCommand.Unsubscribe, LINE_VOLTAGE_ATTRIBUTE, null);
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Unsubscribe, DTMF_LOCAL_LEVEL_ATTRIBUTE, null);
			RequestAttribute(LoopCurrentFeedback, AttributeCode.eCommand.Unsubscribe, LOOP_CURRENT_ATTRIBUTE, null);
			RequestAttribute(RingBackToneDetectedFeedback, AttributeCode.eCommand.Unsubscribe, RING_BACK_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(RingingFeedback, AttributeCode.eCommand.Unsubscribe, RINGING_ATTRIBUTE, null);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Get, AUTO_ANSWER_ATTRIBUTE, null);
			RequestAttribute(AutoAnswerRingCountFeedback, AttributeCode.eCommand.Get, AUTO_ANSWER_RING_COUNT_ATTRIBUTE, null);
			RequestAttribute(AutoDisconnectTypeFeedback, AttributeCode.eCommand.Get, AUTO_DISCONNECT_TYPE_ATTRIBUTE, null);
			RequestAttribute(BusyToneDetectedFeedback, AttributeCode.eCommand.Get, BUSY_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(CallerIdEnabledFeedback, AttributeCode.eCommand.Get, CALLER_ID_ENABLED_ATTRIBUTE, null);
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Get, CALL_STATE_ATTRIBUTE, null);
			RequestAttribute(DialingFeedback, AttributeCode.eCommand.Get, DIALING_ATTRIBUTE, null);
			RequestAttribute(DialToneDetectedFeedback, AttributeCode.eCommand.Get, DIAL_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(DialToneLevelFeedback, AttributeCode.eCommand.Get, DIAL_TONE_LEVEL_ATTRIBUTE, null);
			RequestAttribute(LineFaultConditionFeedback, AttributeCode.eCommand.Get, LINE_FAULT_CONDITION_ATTRIBUTE, null);
			RequestAttribute(HookStateFeedback, AttributeCode.eCommand.Get, HOOK_STATE_ATTRIBUTE, null);
			RequestAttribute(LastNumberDialedFeedback, AttributeCode.eCommand.Get, LAST_NUMBER_DIALED_ATTRIBUTE, null);
			RequestAttribute(LineFaultFeedback, AttributeCode.eCommand.Get, LINE_FAULT_ATTRIBUTE, null);
			RequestAttribute(LineIntrusionFeedback, AttributeCode.eCommand.Get, LINE_INTRUSION_ATTRIBUTE, null);
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Get, LINE_IN_USE_ATTRIBUTE, null);
			RequestAttribute(LineReadyFeedback, AttributeCode.eCommand.Get, LINE_READY_ATTRIBUTE, null);
			RequestAttribute(LineVoltageFeedback, AttributeCode.eCommand.Get, LINE_VOLTAGE_ATTRIBUTE, null);
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Get, DTMF_LOCAL_LEVEL_ATTRIBUTE, null);
			RequestAttribute(LoopCurrentFeedback, AttributeCode.eCommand.Get, LOOP_CURRENT_ATTRIBUTE, null);
			RequestAttribute(RingBackToneDetectedFeedback, AttributeCode.eCommand.Get, RING_BACK_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(RingingFeedback, AttributeCode.eCommand.Get, RINGING_ATTRIBUTE, null);
			RequestAttribute(UseRedialFeedback, AttributeCode.eCommand.Get, USE_REDIAL_ATTRIBUTE, null);
			RequestAttribute(WaitForDialToneFeedback, AttributeCode.eCommand.Get, WAIT_FOR_DIAL_TONE_ATTRIBUTE, null);

			// Subscribe
			RequestAttribute(BusyToneDetectedFeedback, AttributeCode.eCommand.Subscribe, BUSY_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Subscribe, CALL_STATE_ATTRIBUTE, null);
			RequestAttribute(DialingFeedback, AttributeCode.eCommand.Subscribe, DIALING_ATTRIBUTE, null);
			RequestAttribute(DialToneDetectedFeedback, AttributeCode.eCommand.Subscribe, DIAL_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(LineFaultConditionFeedback, AttributeCode.eCommand.Subscribe, LINE_FAULT_CONDITION_ATTRIBUTE, null);
			RequestAttribute(HookStateFeedback, AttributeCode.eCommand.Subscribe, HOOK_STATE_ATTRIBUTE, null);
			RequestAttribute(LastNumberDialedFeedback, AttributeCode.eCommand.Subscribe, LAST_NUMBER_DIALED_ATTRIBUTE, null);
			RequestAttribute(LineFaultFeedback, AttributeCode.eCommand.Subscribe, LINE_FAULT_ATTRIBUTE, null);
			RequestAttribute(LineIntrusionFeedback, AttributeCode.eCommand.Subscribe, LINE_INTRUSION_ATTRIBUTE, null);
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Subscribe, LINE_IN_USE_ATTRIBUTE, null);
			RequestAttribute(LineReadyFeedback, AttributeCode.eCommand.Subscribe, LINE_READY_ATTRIBUTE, null);
			RequestAttribute(LineVoltageFeedback, AttributeCode.eCommand.Subscribe, LINE_VOLTAGE_ATTRIBUTE, null);
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Subscribe, DTMF_LOCAL_LEVEL_ATTRIBUTE, null);
			RequestAttribute(LoopCurrentFeedback, AttributeCode.eCommand.Subscribe, LOOP_CURRENT_ATTRIBUTE, null);
			RequestAttribute(RingBackToneDetectedFeedback, AttributeCode.eCommand.Subscribe, RING_BACK_TONE_DETECTED_ATTRIBUTE, null);
			RequestAttribute(RingingFeedback, AttributeCode.eCommand.Subscribe, RINGING_ATTRIBUTE, null);
		}

		#endregion

		#region Services

		[PublicAPI]
		public void Redial()
		{
			RequestService(REDIAL_SERVICE, null);
		}

		[PublicAPI]
		public void End()
		{
			RequestService(END_SERVICE, null);
		}

		[PublicAPI]
		public void Dial(string number)
		{
			RequestService(DIAL_SERVICE, new Value(number));
		}

		[PublicAPI]
		public void Dtmf(char key)
		{
			RequestService(DTMF_SERVICE, new Value(key));
		}

		[PublicAPI]
		public void Answer()
		{
			RequestService(ANSWER_SERVICE, null);
		}

		#endregion

		#region Attributes

		[PublicAPI]
		public void SetAutoAnswer(bool autoAnswer)
		{
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Set, AUTO_ANSWER_ATTRIBUTE, new Value(autoAnswer));
		}

		[PublicAPI]
		public void ToggleAutoAnswer()
		{
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Toggle, AUTO_ANSWER_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetAutoAnswerRingCount(eAutoAnswerRingCount ringCount)
		{
			Value value = Value.FromObject(ringCount, s_AutoAnswerRingCountSerials);
			RequestAttribute(AutoAnswerRingCountFeedback, AttributeCode.eCommand.Set, AUTO_ANSWER_RING_COUNT_ATTRIBUTE, value);
		}

		[PublicAPI]
		public void SetAutoDisconnectType(eAutoDisconnecType disconnectType)
		{
			Value value = Value.FromObject(disconnectType, s_AutoDisconnectTypeSerials);
			RequestAttribute(AutoDisconnectTypeFeedback, AttributeCode.eCommand.Set, AUTO_DISCONNECT_TYPE_ATTRIBUTE, value);
		}

		[PublicAPI]
		public void SetCallerIdEnabled(bool enabled)
		{
			RequestAttribute(CallerIdEnabledFeedback, AttributeCode.eCommand.Set, CALLER_ID_ENABLED_ATTRIBUTE, new Value(enabled));
		}

		[PublicAPI]
		public void ToggleCallerIdEnabled()
		{
			RequestAttribute(CallerIdEnabledFeedback, AttributeCode.eCommand.Toggle, CALLER_ID_ENABLED_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetDialToneLevel(float level)
		{
			RequestAttribute(DialToneLevelFeedback, AttributeCode.eCommand.Toggle, DIAL_TONE_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementDialToneLevel()
		{
			RequestAttribute(DialToneLevelFeedback, AttributeCode.eCommand.Increment, DIAL_TONE_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementDialToneLevel()
		{
			RequestAttribute(DialToneLevelFeedback, AttributeCode.eCommand.Decrement, DIAL_TONE_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetHookState(eHookState state)
		{
			Value value = Value.FromObject(state, s_HookStateSerials);
			RequestAttribute(HookStateFeedback, AttributeCode.eCommand.Set, HOOK_STATE_ATTRIBUTE, value);
		}

		[PublicAPI]
		public void SetDtmfLocalLevel(float level)
		{
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Set, DTMF_LOCAL_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementDtmfLocalLevel()
		{
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Increment, DTMF_LOCAL_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementDtmfLocalLevel()
		{
			RequestAttribute(DtmfLocalLevelFeedback, AttributeCode.eCommand.Decrement, DTMF_LOCAL_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetUseRedial(bool useRedial)
		{
			RequestAttribute(UseRedialFeedback, AttributeCode.eCommand.Set, USE_REDIAL_ATTRIBUTE, new Value(useRedial));
		}

		[PublicAPI]
		public void ToggleRedial()
		{
			RequestAttribute(UseRedialFeedback, AttributeCode.eCommand.Toggle, USE_REDIAL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetWaitForDialTone(bool waitForDialTone)
		{
			RequestAttribute(WaitForDialToneFeedback, AttributeCode.eCommand.Set, WAIT_FOR_DIAL_TONE_ATTRIBUTE, new Value(waitForDialTone));
		}

		[PublicAPI]
		public void ToggleWaitForDialTone()
		{
			RequestAttribute(WaitForDialToneFeedback, AttributeCode.eCommand.Toggle, WAIT_FOR_DIAL_TONE_ATTRIBUTE, null);
		}

		#endregion

		#region Subscription Feedback

		private void AutoAnswerFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			AutoAnswer = (value["value"] as Value).BoolValue;
		}

		private void AutoAnswerRingCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			AutoAnswerRingCount = (value["value"] as Value).GetObjectValue(s_AutoAnswerRingCountSerials);
		}

		private void AutoDisconnectTypeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			AutoDisconnectType = (value["value"] as Value).GetObjectValue(s_AutoDisconnectTypeSerials);
		}

		private void BusyToneDetectedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			BusyToneDetected = (value["value"] as Value).BoolValue;
		}

		private void CallerIdEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			CallerIdEnabled = (value["value"] as Value).BoolValue;
		}

		private void CallStateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue result = value["value"] as ControlValue;
			ArrayValue callStates = result["callStateInfo"] as ArrayValue;
			ControlValue callState = callStates.Cast<ControlValue>().First();

			State = (callState["state"] as Value).GetObjectValue(s_CallStateSerials);

			Value cid = callState["cid"] as Value;
			string[] cidSplit = cid.GetStringValues().ToArray();

			CallerNumber = cidSplit.Length > 2 ? cidSplit[1] : null;
			CallerName = cidSplit.Length > 3 ? cidSplit[2] : null;
		}

		private void DialingFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Dialing = (value["value"] as Value).BoolValue;
		}

		private void DialToneDetectedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			DialToneDetected = (value["value"] as Value).BoolValue;
		}

		private void DialToneLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			DialToneLevel = (value["value"] as Value).FloatValue;
		}

		private void LineFaultConditionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineFaultCondition = (value["value"] as Value).GetObjectValue(s_LineFaultConditionsSerials);
		}

		private void HookStateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			HookState = (value["value"] as Value).GetObjectValue(s_HookStateSerials);
		}

		private void LastNumberDialedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LastNumberDialed = (value["value"] as Value).StringValue;
		}

		private void LineFaultFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineFault = (value["value"] as Value).BoolValue;
		}

		private void LineIntrusionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineIntrusion = (value["value"] as Value).BoolValue;
		}

		private void LineInUseFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineInUse = (value["value"] as Value).BoolValue;
		}

		private void LineReadyFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineReady = (value["value"] as Value).BoolValue;
		}

		private void LineVoltageFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LineVoltage = (value["value"] as Value).FloatValue;
		}

		private void DtmfLocalLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			DtmfLocalLevel = (value["value"] as Value).FloatValue;
		}

		private void LoopCurrentFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			LoopCurrent = (value["value"] as Value).FloatValue;
		}

		private void RingBackToneDetectedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			RingBackToneDetected = (value["value"] as Value).BoolValue;
		}

		private void RingingFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Ringing = (value["value"] as Value).BoolValue;
		}

		private void UseRedialFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			UseRedial = (value["value"] as Value).BoolValue;
		}

		private void WaitForDialToneFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			WaitForDialTone = (value["value"] as Value).BoolValue;
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

			addRow("Auto Answer", AutoAnswer);
			addRow("Auto Answer Ring Count", AutoAnswerRingCount);
			addRow("Auto Disconnect Type", AutoDisconnectType);
			addRow("Busy Tone Detected", BusyToneDetected);
			addRow("Caller Id Enabled", CallerIdEnabled);
			addRow("Dialing", Dialing);
			addRow("Dial Tone Detected", DialToneDetected);
			addRow("Dial Tone Level", DialToneLevel);
			addRow("Line Fault Condition", LineFaultCondition);
			addRow("Hook State", HookState);
			addRow("Last Number Dialed", LastNumberDialed);
			addRow("Line Fault", LineFault);
			addRow("Line Intrusion", LineIntrusion);
			addRow("Line In Use", LineInUse);
			addRow("Line Ready", LineReady);
			addRow("Line Voltage", LineVoltage);
			addRow("Dtmf Local Level", DtmfLocalLevel);
			addRow("Loop Current", LoopCurrent);
			addRow("Ring Back Tone Detected", RingBackToneDetected);
			addRow("Ringing", Ringing);
			addRow("Use Redial", UseRedial);
			addRow("Wait For Dial Tone", WaitForDialTone);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Redial", "", () => Redial());
			yield return new ConsoleCommand("End", "", () => End());
			yield return new GenericConsoleCommand<string>("Dial", "Dial <NUMBER>", s => Dial(s));
			yield return new GenericConsoleCommand<char>("Dtmf", "Dtmf <KEY>", c => Dtmf(c));
			yield return new ConsoleCommand("Answer", "", () => Answer());

			yield return new GenericConsoleCommand<bool>("SetAutoAnswer", "SetAutoAnswer <true/false>", b => SetAutoAnswer(b));
			yield return new ConsoleCommand("ToggleAutoAnswer", "", () => ToggleAutoAnswer());

			string setAutoAnswerRingCountHelp =
				string.Format("SetAutoAnswerRingCount <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eAutoAnswerRingCount>()));
			yield return new GenericConsoleCommand<eAutoAnswerRingCount>("SetAutoAnswerRingCount", setAutoAnswerRingCountHelp, e => SetAutoAnswerRingCount(e));

			string setAutoDisconnectTypeHelp =
				string.Format("SetAutoDisconnectType <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eAutoDisconnecType>()));
			yield return new GenericConsoleCommand<eAutoDisconnecType>("SetAutoDisconnectType", setAutoDisconnectTypeHelp, e => SetAutoDisconnectType(e));

			yield return new GenericConsoleCommand<bool>("SetCallerIdEnabled", "SetCallerIdEnabled <true/false>", b => SetCallerIdEnabled(b));
			yield return new ConsoleCommand("ToggleCallerIdEnabled", "", () => ToggleCallerIdEnabled());

			yield return new GenericConsoleCommand<float>("SetDialToneLevel", "SetDialToneLevel <LEVEL>", f => SetDialToneLevel(f));
			yield return new ConsoleCommand("IncrementDialToneLevel", "", () => IncrementDialToneLevel());
			yield return new ConsoleCommand("DecrementDialToneLevel", "", () => DecrementDialToneLevel());

			string setHookStateHelp =
				string.Format("SetAutoDisconnectType <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eHookState>()));
			yield return new GenericConsoleCommand<eHookState>("SetHookState", setHookStateHelp, e => SetHookState(e));

			yield return new GenericConsoleCommand<float>("SetDtmfLocalLevel", "SetDtmfLocalLevel <LEVEL>", f => SetDtmfLocalLevel(f));
			yield return new ConsoleCommand("IncrementDtmfLocalLevel", "", () => IncrementDtmfLocalLevel());
			yield return new ConsoleCommand("DecrementDtmfLocalLevel", "", () => DecrementDtmfLocalLevel());

			yield return new GenericConsoleCommand<bool>("SetUseRedial", "SetUseRedial <true/false>", b => SetUseRedial(b));
			yield return new ConsoleCommand("ToggleRedial", "", () => ToggleRedial());

			yield return new GenericConsoleCommand<bool>("SetWaitForDialTone", "SetWaitForDialTone <true/false>", b => SetWaitForDialTone(b));
			yield return new ConsoleCommand("ToggleWaitForDialTone", "", () => ToggleWaitForDialTone());
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
