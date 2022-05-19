using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.Tesira.Controls.State;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls.Dialing.Telephone
{
	public sealed class TiConferenceDeviceControl : AbstractBiampTesiraConferenceDeviceControl
	{
		/// <summary>
		/// Raised when the hold state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnHoldChanged;

		private readonly TiControlStatusBlock m_TiControl;

		[CanBeNull]
		private readonly IBiampTesiraStateDeviceControl m_HoldControl;

		[CanBeNull]
		private readonly IBiampTesiraStateDeviceControl m_DoNotDisturbControl;

		private bool m_Hold;

		private ThinConference m_ActiveConference;
		private readonly SafeCriticalSection m_ActiveSection;
		private string m_LastDialedNumber;

		private TraditionalIncomingCall m_IncomingCall;
		
		/// <summary>
		/// True when we are rejecting a call - prevents call from being added as an answered/active source
		/// </summary>
		private bool m_RejectingCall;

		#region Properties

		/// <summary>
		/// Gets the hold state.
		/// </summary>
		public bool IsOnHold
		{
			get { return m_Hold; }
			private set
			{
				if (value == m_Hold)
					return;

				m_Hold = value;

				Logger.LogSetTo(eSeverity.Informational, "IsOnHold", m_Hold);

				OnHoldChanged.Raise(this, new BoolEventArgs(m_Hold));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="tiControl"></param>
		/// <param name="doNotDisturbControl"></param>
		/// <param name="privacyMuteControl"></param>
		/// <param name="holdControl"></param>
		/// <param name="callInInfo"></param>
		public TiConferenceDeviceControl(int id, Guid uuid, string name, TiControlStatusBlock tiControl,
		                                 IBiampTesiraStateDeviceControl doNotDisturbControl,
		                                 IBiampTesiraStateDeviceControl privacyMuteControl,
		                                 IBiampTesiraStateDeviceControl holdControl,
		                                 IDialContext callInInfo)
			: base(id, uuid, name, tiControl.Device, privacyMuteControl)
		{
			m_ActiveSection = new SafeCriticalSection();

			m_TiControl = tiControl;
			m_HoldControl = holdControl;
			m_DoNotDisturbControl = doNotDisturbControl;
			CallInInfo = callInInfo;

			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CanDial | eConferenceControlFeatures.CanEnd;

			if (m_TiControl != null)
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.AutoAnswer;

			if (m_DoNotDisturbControl != null)
				SupportedConferenceControlFeatures |= eConferenceControlFeatures.DoNotDisturb;

			Subscribe(m_TiControl);
			SubscribeHold(m_HoldControl);
			SubscribeDoNotDisturb(m_DoNotDisturbControl);
		}

		#region Methods

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (dialContext == null)
				throw new ArgumentNullException("dialContext");

			if (dialContext.Protocol == eDialProtocol.Pstn && !string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Unknown && !string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unknown;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			m_LastDialedNumber = dialContext.DialString;
			m_TiControl.Dial(dialContext.DialString);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			m_TiControl.SetAutoAnswer(enabled);
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			if (m_DoNotDisturbControl == null)
			{
				Logger.Log(eSeverity.Error, "Unable to set Do-Not-Disturb - control is null");
				return;
			}

			m_DoNotDisturbControl.SetState(enabled);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnHoldChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_TiControl);
			UnsubscribeHold(m_HoldControl);
			UnsubscribeDoNotDisturb(m_DoNotDisturbControl);

			ClearCurrentConference();
		}

		/// <summary>
		/// Sets the hold state.
		/// </summary>
		/// <param name="hold"></param>
		private void SetHold(bool hold)
		{
			if (m_HoldControl == null)
			{
				Logger.Log(eSeverity.Error, "Unable to hold - control is null");
				return;
			}

			m_HoldControl.SetState(hold);
		}

		private eCallAnswerState TiControlStateToIncomingAnswerState(TiControlStatusBlock.eTiCallState state)
		{

			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
					return eCallAnswerState.Unknown;

				case TiControlStatusBlock.eTiCallState.Dialing:
				case TiControlStatusBlock.eTiCallState.RingBack:
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eCallAnswerState.Unanswered;

				case TiControlStatusBlock.eTiCallState.Idle:
				case TiControlStatusBlock.eTiCallState.Dropped:
					return m_RejectingCall ? eCallAnswerState.Rejected : eCallAnswerState.Ignored;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
					return AutoAnswer ? eCallAnswerState.AutoAnswered : eCallAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		private static eCallAnswerState TiControlStateToOutboundAnswerState(TiControlStatusBlock.eTiCallState state)
		{

			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Idle:
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eCallAnswerState.Unknown;

				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
					return eCallAnswerState.Rejected;

				case TiControlStatusBlock.eTiCallState.Dialing:
				case TiControlStatusBlock.eTiCallState.RingBack:
					return eCallAnswerState.Unanswered;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
					return eCallAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		private static eConferenceStatus TiControlStateToConferenceStatus(TiControlStatusBlock.eTiCallState state)
		{
			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
					return eConferenceStatus.Undefined;

				case TiControlStatusBlock.eTiCallState.Dialing:
					case TiControlStatusBlock.eTiCallState.Ringing:
					return eConferenceStatus.Connecting;

				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
				case TiControlStatusBlock.eTiCallState.Idle:
					return eConferenceStatus.Disconnected;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
				case TiControlStatusBlock.eTiCallState.RingBack:
					return eConferenceStatus.Connected;
				
				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		/// <summary>
		/// Instantiates a new active source.
		/// </summary>
		private void CreateConferenceAndParticipant()
		{
			m_ActiveSection.Enter();

			try
			{
				ClearCurrentConference();
				

				// If there's an incoming call, we're answering that, so pull info from it
				if (m_IncomingCall != null)
				{
					//Update incoming call first
					UpdateIncomingCall(m_IncomingCall);
					m_ActiveConference = ThinConference.FromIncomingCall(m_IncomingCall);
					ClearCurrentIncomingCall();
				}
				else
				{
					m_ActiveConference = new ThinConference();
					m_ActiveConference.Direction = eCallDirection.Outgoing;
					m_ActiveConference.AnswerState = eCallAnswerState.Unknown;
				}

				Subscribe(m_ActiveConference);

				m_ActiveConference.CallType = eCallType.Audio;

				// Setup the conference
				UpdateConference(m_ActiveConference);

				// Clear the hold state between calls
				SetHold(false);
			}
			finally
			{
				m_ActiveSection.Leave();
			}

			AddConference(m_ActiveConference);
		}

		#endregion

		#region Conference

		/// <summary>
		/// Unsubscribes from the current source and clears the field.
		/// </summary>
		private void ClearCurrentConference()
		{
			m_ActiveSection.Enter();

			try
			{
				if (m_ActiveConference == null)
					return;

				UpdateConference(m_ActiveConference);
				Unsubscribe(m_ActiveConference);
				RemoveConference(m_ActiveConference);
				m_ActiveConference = null;

				// Clear the hold state between calls
				SetHold(false);
			}
			finally
			{
				m_ActiveSection.Leave();
			}
		}

		private void Subscribe(ThinConference conference)
		{
			// Only register hold callback if we have a hold control
			if (m_HoldControl != null)
				conference.HoldCallback = HoldCallback;

			conference.ResumeCallback = ResumeCallback;
			conference.SendDtmfCallback = SendDtmfCallback;
			conference.LeaveConferenceCallback = LeaveCallback;
		}

		private void Unsubscribe(ThinConference conference)
		{
			conference.HoldCallback = null;
			conference.ResumeCallback = null;
			conference.SendDtmfCallback = null;
			conference.LeaveConferenceCallback = null;
		}

		private void UpdateConference(ThinConference conference)
		{
			conference.Status = TiControlStateToConferenceStatus(m_TiControl.State);
			
			if (!string.IsNullOrEmpty(m_TiControl.CallerName))
				conference.Name = m_TiControl.CallerName;
			else if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				conference.Name = m_TiControl.CallerNumber;

			if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				conference.Number = m_TiControl.CallerNumber;

			eConferenceStatus status = TiControlStateToConferenceStatus(m_TiControl.State);
			if (IsOnHold)
				status = eConferenceStatus.OnHold;

			conference.Status = status;

			if (conference.Direction != eCallDirection.Incoming)
			{
				if (string.IsNullOrEmpty(conference.Number) &&
					string.IsNullOrEmpty(conference.Name) &&
					!string.IsNullOrEmpty(m_LastDialedNumber))
				{
					conference.Number = m_LastDialedNumber;
					conference.Name = m_LastDialedNumber;
					m_LastDialedNumber = null;
				}

				conference.Direction = eCallDirection.Outgoing;
			}

			//Don't update answer state for incoming calls - those are preserved from the incoming call
			if (conference.Direction != eCallDirection.Incoming)
			{
				// Don't update the answer state if we can't determine the current answer state
				// Also, once we have a valid answer state, don't overrite it (prevents rejected/ignored from overrideing answered)4
				eCallAnswerState answerState = TiControlStateToOutboundAnswerState(m_TiControl.State);
				if ((conference.AnswerState == eCallAnswerState.Unknown || conference.AnswerState == eCallAnswerState.Unanswered)
					&& answerState != eCallAnswerState.Unknown)
					conference.AnswerState = answerState;
			}

			// Start/End
			switch (status)
			{
				case eConferenceStatus.Connected:
					conference.StartTime = conference.StartTime ?? IcdEnvironment.GetUtcTime();
					break;
				case eConferenceStatus.Disconnected:
					conference.EndTime = conference.EndTime ?? IcdEnvironment.GetUtcTime();
					break;
			}
		}

		private void HoldCallback(ThinConference sender)
		{
			SetHold(true);
		}

		private void ResumeCallback(ThinConference sender)
		{
			SetHold(false);
		}

		private void SendDtmfCallback(ThinConference sender, string data)
		{
			foreach (char digit in data)
				m_TiControl.Dtmf(digit);
		}

		private void LeaveCallback(ThinConference sender)
		{
			// Ends the active call.
			m_TiControl.End();
		}

		#endregion

		#region Participant

		/// <summary>
		/// Creates a source if a call is active but no source exists yet. Clears the source if an existing call becomes inactive.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		private void CreateOrRemoveConferenceForCallState(TiControlStatusBlock.eTiCallState state)
		{
			m_ActiveSection.Enter();

			try
			{

				switch (state)
				{
					case TiControlStatusBlock.eTiCallState.Init:
					case TiControlStatusBlock.eTiCallState.Dropped:
					case TiControlStatusBlock.eTiCallState.Idle:
					case TiControlStatusBlock.eTiCallState.Fault:
					case TiControlStatusBlock.eTiCallState.ErrorTone:
						ClearCurrentConference();
						ClearCurrentIncomingCall();
						break;

					case TiControlStatusBlock.eTiCallState.Dialing:
					case TiControlStatusBlock.eTiCallState.RingBack:
					case TiControlStatusBlock.eTiCallState.BusyTone:
					case TiControlStatusBlock.eTiCallState.Connected:
					case TiControlStatusBlock.eTiCallState.ConnectedMuted:
						// If we are rejecting a call, don't create a participant
						if (!m_RejectingCall)
						{
							if (m_ActiveConference == null)
								CreateConferenceAndParticipant();
							else
								UpdateConference(m_ActiveConference);
						}
						break;
					case TiControlStatusBlock.eTiCallState.Ringing:
						if (m_IncomingCall == null)
							CreateIncomingCall();
						else
							UpdateIncomingCall(m_IncomingCall);
						break;
					
				}
			}
			finally
			{
				m_ActiveSection.Leave();
			}
		}

		#endregion

		#region Incoming Calls

		private void UpdateIncomingCall(TraditionalIncomingCall call)
		{
			if (!string.IsNullOrEmpty(m_TiControl.CallerName))
				call.Name = m_TiControl.CallerName;

			if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				call.Number = m_TiControl.CallerNumber;

			call.Name = call.Name ?? call.Number;

			// Don't update the answer state if we can't determine the current answer state
			// Also, once we have a valid answer state, don't overrite it (prevents rejected/ignored from overrideing answered)
			eCallAnswerState answerState = TiControlStateToIncomingAnswerState(m_TiControl.State);
			if ((call.AnswerState == eCallAnswerState.Unknown || call.AnswerState == eCallAnswerState.Unanswered)
			    &&answerState != eCallAnswerState.Unknown)
				call.AnswerState = answerState;
		}

		private void CreateIncomingCall()
		{
			m_ActiveSection.Enter();
			try
			{
				ClearCurrentConference();

				ClearCurrentIncomingCall();

				m_IncomingCall = new TraditionalIncomingCall(eCallType.Audio);
				Subscribe(m_IncomingCall);
				UpdateIncomingCall(m_IncomingCall);

				SetHold(false);
			}
			finally
			{
				m_ActiveSection.Leave();
			}

			AddIncomingCall(m_IncomingCall);
		}

		private void ClearCurrentIncomingCall()
		{
			m_ActiveSection.Enter();
			try
			{
				if (m_IncomingCall == null)
				{
					m_RejectingCall = false;
					return;
				}

				UpdateIncomingCall(m_IncomingCall);
				Unsubscribe(m_IncomingCall);
				RemoveIncomingCall(m_IncomingCall);
				m_IncomingCall = null;

				SetHold(false);

				m_RejectingCall = false;
			}
			finally
			{
				m_ActiveSection.Leave();
			}
		}

		private void Subscribe(TraditionalIncomingCall call)
		{
			call.AnswerCallback += AnswerCallback;
			call.RejectCallback += RejectCallback;
		}

		private void Unsubscribe(TraditionalIncomingCall call)
		{
			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			m_TiControl.Answer();
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		/// <param name="sender"></param>
		private void RejectCallback(IIncomingCall sender)
		{
			m_RejectingCall = true;
			SetHold(true);
			m_TiControl.SetHookState(TiControlStatusBlock.eHookState.OffHook);
			m_TiControl.SetHookState(TiControlStatusBlock.eHookState.OnHook);
			SetHold(false);
		}

		#endregion

		#region Hold Control Callbacks

		/// <summary>
		/// Subscribe to the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void SubscribeHold(IBiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			holdControl.OnStateChanged += HoldControlOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void UnsubscribeHold(IBiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			holdControl.OnStateChanged -= HoldControlOnStateChanged;
		}

		/// <summary>
		/// Called when the hold control state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void HoldControlOnStateChanged(object sender, BoolEventArgs args)
		{
			IsOnHold = args.Data;

			UpdateConference(m_ActiveConference);
		}

		#endregion

		#region Do Not Disturb Callbacks

		private void SubscribeDoNotDisturb(IBiampTesiraStateDeviceControl doNotDisturbControl)
		{
			if (doNotDisturbControl == null)
				return;

			doNotDisturbControl.OnStateChanged += DoNotDisturbControlOnStateChanged;
		}

		private void UnsubscribeDoNotDisturb(IBiampTesiraStateDeviceControl doNotDisturbControl)
		{
			if (doNotDisturbControl == null)
				return;

			doNotDisturbControl.OnStateChanged -= DoNotDisturbControlOnStateChanged;
		}

		private void DoNotDisturbControlOnStateChanged(object sender, BoolEventArgs args)
		{
			DoNotDisturb = args.Data;
		}

		#endregion

		#region Attribute Interface Callbacks

		/// <summary>
		/// Subscribe to the dialer block events.
		/// </summary>
		/// <param name="attributeInterface"></param>
		private void Subscribe(TiControlStatusBlock attributeInterface)
		{
			attributeInterface.OnAutoAnswerChanged += AttributeInterfaceOnAutoAnswerChanged;
			attributeInterface.OnCallStateChanged += AttributeInterfaceOnCallStateChanged;
			attributeInterface.OnCallerNameChanged += AttributeInterfaceOnCallerNameChanged;
			attributeInterface.OnCallerNumberChanged += AttributeInterfaceOnCallerNumberChanged;
		}

		/// <summary>
		/// Unsubscribe to the dialer block events.
		/// </summary>
		/// <param name="attributeInterface"></param>
		private void Unsubscribe(TiControlStatusBlock attributeInterface)
		{
			attributeInterface.OnAutoAnswerChanged -= AttributeInterfaceOnAutoAnswerChanged;
			attributeInterface.OnCallStateChanged -= AttributeInterfaceOnCallStateChanged;
			attributeInterface.OnCallerNameChanged -= AttributeInterfaceOnCallerNameChanged;
			attributeInterface.OnCallerNumberChanged -= AttributeInterfaceOnCallerNumberChanged;
		}

		private void AttributeInterfaceOnCallerNumberChanged(object sender, StringEventArgs stringEventArgs)
		{
			if (m_IncomingCall != null)
				m_IncomingCall.Number = stringEventArgs.Data;
			if (m_ActiveConference != null)
				UpdateConference(m_ActiveConference);
		}

		private void AttributeInterfaceOnCallerNameChanged(object sender, StringEventArgs stringEventArgs)
		{
			if (m_IncomingCall != null)
				m_IncomingCall.Name = stringEventArgs.Data;
			if (m_ActiveConference != null)
				UpdateConference(m_ActiveConference);
		}

		private void AttributeInterfaceOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			AutoAnswer = args.Data;
		}

		private void AttributeInterfaceOnCallStateChanged(TiControlStatusBlock sender, TiControlStatusBlock.eTiCallState callState)
		{
			CreateOrRemoveConferenceForCallState(callState);
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

			addRow("IsOnHold", IsOnHold);
			addRow("Hold Control", m_Hold);
			addRow("DoNotDisturb Control", m_DoNotDisturbControl);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetHold", "SetHold <true/false>", h => SetHold(h));
		}

		/// <summary>
		/// Workaround for unverifiable code warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
