using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.EventArgs;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Dialing
{
	public sealed class QSysPotsTraditionalConferenceControl : AbstractConferenceDeviceControl<QSysCoreDevice, ThinConference>,
	                                                           IQSysKrangControl
	{
		private const float TOLERANCE = 0.0001f;
		private const string STATUS_IDLE = "idle";
		private const string STATUS_NORMAL_CLEARING = "normal clearing";
		private const string STATUS_DISCONNECTED = "disconnected";
		private const string STATUS_DIALING = "dialing";
		private const string STATUS_INCOMING_CALL = "incoming call";
		private const string STATUS_CONNECTED = "connected";

		private enum eControlStatus
		{
			Undefined,
			Disconnected,
			Connected,
			Dialing,
			Incoming
		}

		#region Events

		/// <summary>
		/// Raised when an incoming call is added to the conference component.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when an incoming call is removed from the conference component.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		#region Fields

		[CanBeNull] private readonly BooleanNamedControl m_HoldControl;

		[CanBeNull] private readonly BooleanNamedControl m_PrivacyMuteControl;

		[CanBeNull] private readonly PotsNamedComponent m_PotsComponent;

		private readonly SafeCriticalSection m_ConferenceSourceCriticalSection;
		private readonly string m_Name;

		private ThinConference m_Conference;
		private TraditionalIncomingCall m_IncomingCall;

		#endregion

		#region Properties

		private ThinConference Conference
		{
			get { return m_ConferenceSourceCriticalSection.Execute(() => m_Conference); }
			set
			{
				ThinConference removed;

				m_ConferenceSourceCriticalSection.Enter();

				try
				{
					if (value == m_Conference)
						return;

					removed = m_Conference;

					Unsubscribe(m_Conference);
					m_Conference = value;
					Subscribe(m_Conference);
				}
				finally
				{
					m_ConferenceSourceCriticalSection.Leave();
				}

				if (removed != null)
					OnConferenceRemoved.Raise(this, removed);

				if (value != null)
					OnConferenceAdded.Raise(this, value);
			}
		}

		private TraditionalIncomingCall IncomingCall
		{
			get { return m_ConferenceSourceCriticalSection.Execute(() => m_IncomingCall); }
			set
			{
				IIncomingCall removed;

				m_ConferenceSourceCriticalSection.Enter();

				try
				{
					if (value == m_IncomingCall)
						return;

					removed = m_IncomingCall;

					Unsubscribe(m_IncomingCall);
					m_IncomingCall = value;
					Subscribe(m_IncomingCall);
				}
				finally
				{
					m_ConferenceSourceCriticalSection.Leave();
				}

				if (removed != null)
					OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(removed));

				if (value != null)
					OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(value));
			}
		}

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio; } }

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.IsNullOrEmpty(m_Name) ? base.Name : m_Name; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		public QSysPotsTraditionalConferenceControl(int id, Guid uuid, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id, uuid)
		{
			m_Name = friendlyName;
			m_ConferenceSourceCriticalSection = new SafeCriticalSection();

			string voipName = XmlUtils.TryReadChildElementContentAsString(xml, "ControlName");
			string privacyMuteName = XmlUtils.TryReadChildElementContentAsString(xml, "PrivacyMuteControl");
			string holdName = XmlUtils.TryReadChildElementContentAsString(xml, "HoldControl");
			string potsNumber = XmlUtils.TryReadChildElementContentAsString(xml, "PotsNumber");

			// Load volume/mute controls
			m_PotsComponent = context.LazyLoadNamedComponent<PotsNamedComponent>(voipName);
			m_PrivacyMuteControl = context.LazyLoadNamedControl<BooleanNamedControl>(privacyMuteName);
			m_HoldControl = context.LazyLoadNamedControl<BooleanNamedControl>(holdName);

			SupportedConferenceControlFeatures |= eConferenceControlFeatures.Dtmf |
			                               eConferenceControlFeatures.CanDial |
			                               eConferenceControlFeatures.CanEnd;

			if (m_PotsComponent != null)
			{
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.AutoAnswer;
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.DoNotDisturb;
			}

			if (m_PrivacyMuteControl != null)
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.PrivacyMute;

			if (m_HoldControl != null)
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.Hold;

			CallInInfo =
				new DialContext
				{
					Protocol = eDialProtocol.Pstn,
					CallType = eCallType.Audio,
					DialString = potsNumber
				};

			Subscribe();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe();

			Conference = null;
			IncomingCall = null;

			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			base.DisposeFinal(disposing);
		}

		#region Public Methods

		public override IEnumerable<ThinConference> GetConferences()
		{
			yield break;
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (dialContext.CallType == eCallType.Video || string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Pstn || dialContext.Protocol == eDialProtocol.Sip)
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Unknown)
				return eDialContextSupport.Unknown;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to dial - POTS component is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Dialing {0}", dialContext.DialString);

			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_NUMBER, dialContext.DialString);
			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_CONNECT);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to set DoNotDisturb - POTS component is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Setting Do Not Disturb to {0}", enabled);
			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_DND, enabled ? "1" : "0");
		}

		public override void SetAutoAnswer(bool enabled)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to set AutoAnswer - POTS component is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Setting Auto Answer to {0}", enabled);
			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_AUTOANSWER, enabled ? "1" : "0");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			if (m_PrivacyMuteControl == null)
			{
				Logger.Log(eSeverity.Error, "Unable to set Privacymute - PrivacyMute control is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Setting Privacy Mute to {0}", enabled);
			m_PrivacyMuteControl.SetValue(enabled);
		}

		public override void SetCameraMute(bool mute)
		{
			throw new NotSupportedException();
		}

		public override void StartPersonalMeeting()
		{
			throw new NotSupportedException();
		}

		public override void EnableCallLock(bool enabled)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

		private void ParseDnd(ControlValueUpdateEventArgs args)
		{
			DoNotDisturb = Math.Abs(args.ValuePosition) > TOLERANCE;
		}

		private void ParseCidNumber(ControlValueUpdateEventArgs args)
		{
			if (string.IsNullOrEmpty(args.ValueString))
				return;

			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				if (m_IncomingCall != null)
					m_IncomingCall.Number = args.ValueString;
				if (m_Conference != null)
					m_Conference.Number = args.ValueString;
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private void ParseCidName(ControlValueUpdateEventArgs args)
		{
			if (string.IsNullOrEmpty(args.ValueString))
				return;

			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				if (m_IncomingCall != null)
					m_IncomingCall.Name = args.ValueString;
				if (m_Conference != null)
					m_Conference.Name = args.ValueString;
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private void ParseAutoAnswer(ControlValueUpdateEventArgs args)
		{
			AutoAnswer = Math.Abs(args.ValuePosition) > TOLERANCE;
		}

		private void ParseCallStatus(ControlValueUpdateEventArgs args)
		{
			Logger.Log(eSeverity.Debug, "Call Status: {0}", args.ValueString);
			eConferenceStatus callStatus = QSysStatusToConferenceSourceStatus(args.ValueString);
			eControlStatus controlStatus = QSysStatusToControlStatus(args.ValueString);

			var incomingCall = IncomingCall;
			var source = Conference;

			if (controlStatus == eControlStatus.Disconnected)
			{
				if (source != null)
				{
					source.Status = callStatus;
					source.EndTime = IcdEnvironment.GetUtcTime();
				}
				Conference = null;
				IncomingCall = null;
				return;
			}

			if (controlStatus == eControlStatus.Incoming)
			{
				if (incomingCall == null)
				{
					CreateIncomingCall();
					incomingCall = IncomingCall;
				}
				incomingCall.AnswerState = eCallAnswerState.Unanswered;
			}
			else
			{
				if (source == null)
				{
					CreateConferenceSource();
					source = Conference;
				}

				source.Status = callStatus;

				if (controlStatus == eControlStatus.Dialing)
				{
					source.Direction = eCallDirection.Outgoing;
					string number = GetNumberFromDialingStatus(args.ValueString);
					if (!string.IsNullOrEmpty(number))
						source.Number = number;

					if (incomingCall != null && incomingCall.AnswerState == eCallAnswerState.Unanswered)
						incomingCall.AnswerState = eCallAnswerState.Ignored;
				}

				if (callStatus == eConferenceStatus.Connected)
				{
					if (source.StartTime == null)
						source.StartTime = IcdEnvironment.GetUtcTime();

					if (incomingCall != null && incomingCall.AnswerState == eCallAnswerState.Unanswered)
						incomingCall.AnswerState = eCallAnswerState.AutoAnswered;
				}
			}
		}

		private void CreateConferenceSource()
		{
			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				if (Conference != null)
					return;

				IncomingCall = null;
				Conference = new ThinConference
				{
					CallType = eCallType.Audio
				};
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private void CreateIncomingCall()
		{
			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				if (IncomingCall != null)
					return;

				Conference = null;
				IncomingCall = new TraditionalIncomingCall(eCallType.Audio);
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private static eConferenceStatus QSysStatusToConferenceSourceStatus(string qsysStatus)
		{
			string status = qsysStatus.Split('-', 2).ToArray()[0].Trim();

			switch (status.ToLower())
			{
				case STATUS_IDLE:
				case STATUS_NORMAL_CLEARING:
				case STATUS_DISCONNECTED:
					return eConferenceStatus.Disconnected;
				case STATUS_DIALING:
				case STATUS_INCOMING_CALL:
					return eConferenceStatus.Connecting;
				case STATUS_CONNECTED:
					return eConferenceStatus.Connected;
				default:
					return eConferenceStatus.Undefined;
			}
		}

		private static eControlStatus QSysStatusToControlStatus(string qsysStatus)
		{
			string status = qsysStatus.Split('-', 2).ToArray()[0].Trim();

			switch (status.ToLower())
			{
				case STATUS_IDLE:
				case STATUS_NORMAL_CLEARING:
				case STATUS_DISCONNECTED:
					return eControlStatus.Disconnected;
				case STATUS_DIALING:
					return eControlStatus.Dialing;
				case STATUS_INCOMING_CALL:
					return eControlStatus.Incoming;
				case STATUS_CONNECTED:
					return eControlStatus.Connected;
				default:
					return eControlStatus.Undefined;
			}
		}

		private static string GetNumberFromDialingStatus(string qsysStatus)
		{
			return qsysStatus.Split('-', 2).ToArray()[1].Trim();
		}

		#endregion

		#region Component Callbacks

		private void Subscribe()
		{
			if (m_PotsComponent != null)
				m_PotsComponent.OnControlValueUpdated += PotsComponentControlValueUpdated;

			if (m_PrivacyMuteControl != null)
				m_PrivacyMuteControl.OnValueUpdated += PrivacyMuteControlOnValueUpdated;

			if (m_HoldControl != null)
				m_HoldControl.OnValueUpdated += HoldControlOnValueUpdated;
		}

		private void Unsubscribe()
		{
			if (m_PotsComponent != null)
				m_PotsComponent.OnControlValueUpdated -= PotsComponentControlValueUpdated;

			if (m_PrivacyMuteControl != null)
				m_PrivacyMuteControl.OnValueUpdated -= PrivacyMuteControlOnValueUpdated;

			if (m_HoldControl != null)
				m_HoldControl.OnValueUpdated -= HoldControlOnValueUpdated;
		}

		private void PotsComponentControlValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			INamedComponentControl control = sender as INamedComponentControl;
			if (control == null)
				throw new
					InvalidOperationException(
					string.Format("POTS Dialing Device {0}:{1} - PotsComponentOnControlValueUpdated sender isn't an INamedComponentControl",
					              Id, Name));

			switch (control.Name)
			{
				case PotsNamedComponent.CONTROL_CALL_STATUS:
					ParseCallStatus(e);
					break;
				case PotsNamedComponent.CONTROL_CALL_AUTOANSWER:
					ParseAutoAnswer(e);
					break;
				case PotsNamedComponent.CONTROL_CALL_DND:
					ParseDnd(e);
					break;
				case PotsNamedComponent.CONTROL_CALL_CID_NAME:
					ParseCidName(e);
					break;
				case PotsNamedComponent.CONTROL_CALL_CID_NUMBER:
					ParseCidNumber(e);
					break;
			}
		}

		private void PrivacyMuteControlOnValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			PrivacyMuted = Math.Abs(e.ValueRaw) > TOLERANCE;
		}

		private void HoldControlOnValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			bool onHold = Math.Abs(e.ValueRaw) > TOLERANCE;

			ThinConference conference = Conference;
			if (conference == null)
				return;

			if (conference.Status == eConferenceStatus.Connected && onHold)
				conference.Status = eConferenceStatus.OnHold;
			else if (conference.Status == eConferenceStatus.OnHold && !onHold)
				conference.Status = eConferenceStatus.Connected;
		}

		#endregion

		#region Participant Callbacks

		private void Subscribe(ThinConference participant)
		{
			if (participant == null)
				return;

			participant.LeaveConferenceCallback += ConferenceSourceHangupCallback;
			participant.HoldCallback += ConferenceSourceHoldCallback;
			participant.ResumeCallback += ConferenceSourceResumeCallback;
			participant.SendDtmfCallback += ConferenceSourceSendDtmfCallback;
		}

		private void Unsubscribe(ThinConference participant)
		{
			if (participant == null)
				return;

			participant.LeaveConferenceCallback = null;
			participant.HoldCallback = null;
			participant.ResumeCallback = null;
			participant.SendDtmfCallback = null;
		}

		private void ConferenceSourceHangupCallback(ThinConference sender)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to hangup - POTS control is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Hanging up participant {0}", sender.Number);
			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_DISCONNECT);
		}

		private void ConferenceSourceHoldCallback(ThinConference sender)
		{
			if (m_HoldControl == null)
			{
				Logger.Log(eSeverity.Error, "Unable to hold call - Hold control is null");
				return;
			}

			//todo: Verify call is in a state to hold?
			Logger.Log(eSeverity.Debug, "Holding participant {0}", sender.Number);
			m_HoldControl.SetValue(true);
		}

		private void ConferenceSourceResumeCallback(ThinConference sender)
		{
			if (m_HoldControl == null)
			{
				Logger.Log(eSeverity.Error, "Unable to resume call - Hold control is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Resuming participant {0}", sender.Number);
			m_HoldControl.SetValue(false);
		}

		private void ConferenceSourceSendDtmfCallback(ThinConference sender, string dtmf)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send DTMF - POTS component is null");
				return;
			}

			// todo: bail if not off hook?

			foreach (char c in dtmf)
			{
				string controlName;
				switch (c)
				{
					case '0':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_0;
						break;
					case '1':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_1;
						break;
					case '2':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_2;
						break;
					case '3':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_3;
						break;
					case '4':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_4;
						break;
					case '5':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_5;
						break;
					case '6':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_6;
						break;
					case '7':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_7;
						break;
					case '8':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_8;
						break;
					case '9':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_9;
						break;
					case '*':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_STAR;
						break;
					case '#':
						controlName = PotsNamedComponent.CONTROL_CALL_PINPAD_POUND;
						break;
					default:
						throw new ArgumentException(string.Format("POTS Dialing Device {0} - DTMF code {1} not supported", this, c));
				}

				Logger.Log(eSeverity.Debug, "Sending DTMF {0}", c);
				m_PotsComponent.Trigger(controlName);
			}
		}

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe([CanBeNull] TraditionalIncomingCall call)
		{
			if (call == null)
				return;

			call.AnswerCallback += IncomingCallAnswerCallback;
			call.RejectCallback += IncomingCallRejectCallback;
		}

		private void Unsubscribe([CanBeNull] TraditionalIncomingCall call)
		{
			if (call == null)
				return;

			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void IncomingCallAnswerCallback(IIncomingCall sender)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to answer - POTS control is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Answering incoming call {0}", sender.Number);
			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_CONNECT);

			sender.AnswerState = eCallAnswerState.Answered;
		}

		private void IncomingCallRejectCallback(IIncomingCall sender)
		{
			if (m_PotsComponent == null)
			{
				Logger.Log(eSeverity.Error, "Unable to reject - POTS control is null");
				return;
			}

			Logger.Log(eSeverity.Debug, "Rejecting incoming call {0}", sender.Number);
			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_DISCONNECT);

			sender.AnswerState = eCallAnswerState.Ignored;
		}

		#endregion
	}
}
