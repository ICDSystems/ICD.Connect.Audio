using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Dialing
{
	public sealed class QSysPotsTraditionalConferenceControl : AbstractTraditionalConferenceDeviceControl<QSysCoreDevice>,
	                                                           IQSysKrangControl
	{
		private const float TOLERANCE = 0.0001f;

		#region Events

		/// <summary>
		/// Raised when an incoming call is added to the conference component.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when an incoming call is removed from the conference component.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#endregion

		#region Fields

		[CanBeNull] private readonly BooleanNamedControl m_HoldControl;

		[CanBeNull] private readonly BooleanNamedControl m_PrivacyMuteControl;

		[CanBeNull] private readonly PotsNamedComponent m_PotsComponent;

		private readonly SafeCriticalSection m_ConferenceSourceCriticalSection;
		private readonly string m_Name;

		private ThinTraditionalParticipant m_Participant;
		private ThinIncomingCall m_IncomingCall;

		#endregion

		#region Properties

		private ThinTraditionalParticipant Participant
		{
			get { return m_ConferenceSourceCriticalSection.Execute(() => m_Participant); }
			set
			{
				ITraditionalParticipant removed;

				m_ConferenceSourceCriticalSection.Enter();

				try
				{
					if (value == m_Participant)
						return;

					removed = m_Participant;

					Unsubscribe(m_Participant);
					m_Participant = value;
					Subscribe(m_Participant);
				}
				finally
				{
					m_ConferenceSourceCriticalSection.Leave();
				}

				if (removed != null)
					RemoveParticipant(removed);

				if (value != null)
					AddParticipant(value);
			}
		}

		private ThinIncomingCall IncomingCall
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
					OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(m_IncomingCall));

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
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		public QSysPotsTraditionalConferenceControl(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id)
		{
			m_Name = friendlyName;
			m_ConferenceSourceCriticalSection = new SafeCriticalSection();

			string voipName = XmlUtils.TryReadChildElementContentAsString(xml, "ControlName");
			string privacyMuteName = XmlUtils.TryReadChildElementContentAsString(xml, "PrivacyMuteControl");
			string holdName = XmlUtils.TryReadChildElementContentAsString(xml, "HoldControl");

			// Load volume/mute controls
			m_PotsComponent = context.LazyLoadNamedComponent<PotsNamedComponent>(voipName);
			m_PrivacyMuteControl = context.LazyLoadNamedControl<BooleanNamedControl>(privacyMuteName);
			m_HoldControl = context.LazyLoadNamedControl<BooleanNamedControl>(holdName);

			Subscribe();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe();

			Participant = null;
			IncomingCall = null;

			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			base.DisposeFinal(disposing);
		}

		#region Public Methods

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
				Log(eSeverity.Error, "Unable to dial - POTS component is null");
				return;
			}

			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_NUMBER, dialContext.DialString);
			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_CONNECT);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to set DoNotDisturb - POTS component is null");
				return;
			}

			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_DND, enabled ? "1" : "0");
		}

		public override void SetAutoAnswer(bool enabled)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to set AutoAnswer - POTS component is null");
				return;
			}

			m_PotsComponent.SetValue(PotsNamedComponent.CONTROL_CALL_AUTOANSWER, enabled ? "1" : "0");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			if (m_PrivacyMuteControl == null)
			{
				Log(eSeverity.Error, "Unable to set Privacymute - PrivacyMute control is null");
				return;
			}

			m_PrivacyMuteControl.SetValue(enabled);
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
				if (m_Participant != null)
					m_Participant.Number = args.ValueString;
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
				if (m_Participant != null)
					m_Participant.Name = args.ValueString;
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
			Parent.Log(eSeverity.Debug, "Call Status: {0}", args.ValueString);
			eParticipantStatus callStatus = QSysStatusToConferenceSourceStatus(args.ValueString);

			var incomingCall = IncomingCall;
			var source = Participant;

			if (callStatus == eParticipantStatus.Disconnected || callStatus == eParticipantStatus.Idle)
			{
				if (source != null)
				{
					source.Status = callStatus;
					source.End = DateTime.Now;
				}
				Participant = null;
				IncomingCall = null;
				return;
			}

			if (callStatus == eParticipantStatus.Ringing)
			{
				if (incomingCall == null)
				{
					CreateIncomingCall();
					incomingCall = IncomingCall;
				}
				incomingCall.Direction = eCallDirection.Incoming;
				incomingCall.AnswerState = eCallAnswerState.Unanswered;
			}
			else
			{
				if (source == null)
				{
					CreateConferenceSource();
					source = Participant;
				}

				source.Status = callStatus;

				if (callStatus == eParticipantStatus.Dialing)
				{
					source.Direction = eCallDirection.Outgoing;
					string number = GetNumberFromDialingStatus(args.ValueString);
					if (!string.IsNullOrEmpty(number))
						source.Number = number;

					if (incomingCall != null && incomingCall.AnswerState == eCallAnswerState.Unanswered)
						incomingCall.AnswerState = eCallAnswerState.Ignored;
				}

				if (callStatus == eParticipantStatus.Connected)
				{
					if (source.Start == null)
						source.Start = DateTime.Now;

					if (incomingCall != null && incomingCall.AnswerState == eCallAnswerState.Unanswered)
						incomingCall.AnswerState = eCallAnswerState.Autoanswered;
				}
			}
		}

		private void CreateConferenceSource()
		{
			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				if (Participant != null)
					return;

				IncomingCall = null;
				Participant = new ThinTraditionalParticipant {CallType = eCallType.Audio};
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

				Participant = null;
				IncomingCall = new ThinIncomingCall {Direction = eCallDirection.Incoming};
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private static eParticipantStatus QSysStatusToConferenceSourceStatus(string qsysStatus)
		{
			string status = qsysStatus.Split('-', 2).ToArray()[0].Trim();

			switch (status.ToLower())
			{
				case "idle":
					return eParticipantStatus.Disconnected;
				case "normal clearing":
					return eParticipantStatus.Disconnected;
				case "disconnected":
					return eParticipantStatus.Disconnected;
				case "dialing":
					return eParticipantStatus.Dialing;
				case "connected":
					return eParticipantStatus.Connected;
				case "incoming call":
					return eParticipantStatus.Ringing;
				default:
					return eParticipantStatus.Undefined;
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

			ThinTraditionalParticipant source = Participant;
			if (source == null)
				return;

			if (source.Status == eParticipantStatus.Connected && onHold)
				source.Status = eParticipantStatus.OnHold;
			else if (source.Status == eParticipantStatus.OnHold && !onHold)
				source.Status = eParticipantStatus.Connected;
		}

		#endregion

		#region Participant Callbacks

		private void Subscribe(ThinTraditionalParticipant traditionalParticipant)
		{
			if (m_Participant == null)
				return;

			traditionalParticipant.HangupCallback += ConferenceSourceHangupCallback;
			traditionalParticipant.HoldCallback += ConferenceSourceHoldCallback;
			traditionalParticipant.ResumeCallback += ConferenceSourceResumeCallback;
			traditionalParticipant.SendDtmfCallback += ConferenceSourceSendDtmfCallback;
		}

		private void Unsubscribe(ThinTraditionalParticipant traditionalParticipant)
		{
			if (traditionalParticipant == null)
				return;

			traditionalParticipant.HangupCallback = null;
			traditionalParticipant.HoldCallback = null;
			traditionalParticipant.ResumeCallback = null;
			traditionalParticipant.SendDtmfCallback = null;
		}

		private void ConferenceSourceHangupCallback(ThinTraditionalParticipant sender)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to handup - POTS control is null");
				return;
			}

			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_DISCONNECT);
		}

		private void ConferenceSourceHoldCallback(ThinTraditionalParticipant sender)
		{
			if (m_HoldControl == null)
			{
				Log(eSeverity.Error, "Unable to hold call - Hold control is null");
				return;
			}

			//todo: Verify call is in a state to hold?
			m_HoldControl.SetValue(true);
		}

		private void ConferenceSourceResumeCallback(ThinTraditionalParticipant sender)
		{
			if (m_HoldControl == null)
			{
				Log(eSeverity.Error, "Unable to resume call - Hold control is null");
				return;
			}

			m_HoldControl.SetValue(false);
		}

		private void ConferenceSourceSendDtmfCallback(ThinTraditionalParticipant sender, string dtmf)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to send DTMF - POTS component is null");
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

				m_PotsComponent.Trigger(controlName);
			}
		}

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe(ThinIncomingCall call)
		{
			call.AnswerCallback += IncomingCallAnswerCallback;
			call.RejectCallback += IncomingCallRejectCallback;
		}

		private void Unsubscribe(ThinIncomingCall call)
		{
			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void IncomingCallAnswerCallback(ThinIncomingCall sender)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to answer - POTS control is null");
				return;
			}

			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_CONNECT);

			if (sender != null)
				sender.AnswerState = eCallAnswerState.Answered;
		}

		private void IncomingCallRejectCallback(ThinIncomingCall sender)
		{
			if (m_PotsComponent == null)
			{
				Log(eSeverity.Error, "Unable to answer - POTS control is null");
				return;
			}

			m_PotsComponent.Trigger(PotsNamedComponent.CONTROL_CALL_CONNECT);

			if (sender != null)
				sender.AnswerState = eCallAnswerState.Ignored;
		}

		#endregion
	}
}
