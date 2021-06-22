using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.Tesira.Controls.State;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls.Dialing.VoIP
{
	public sealed class VoIpConferenceDeviceControl : AbstractBiampTesiraConferenceDeviceControl
	{
		private readonly VoIpControlStatusLine m_Line;

		private readonly Dictionary<int, ThinConference> m_AppearanceConferences;
		private readonly Dictionary<int, IIncomingCall> m_AppearanceIncomingCalls; 
		private readonly SafeCriticalSection m_AppearanceConferencesSection;
		private string m_LastDialedNumber;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="line"></param>
		/// <param name="privacyMuteControl"></param>
		/// <param name="callInInfo"></param>
		public VoIpConferenceDeviceControl(int id, Guid uuid, string name, VoIpControlStatusLine line,
		                                   IBiampTesiraStateDeviceControl privacyMuteControl,
		                                   IDialContext callInInfo)
			: base(id, uuid, name, line.Device, privacyMuteControl)
		{
			m_AppearanceConferences = new Dictionary<int, ThinConference>();
			m_AppearanceIncomingCalls = new Dictionary<int, IIncomingCall>();
			m_AppearanceConferencesSection = new SafeCriticalSection();

			m_Line = line;
			CallInInfo = callInInfo;

			SupportedConferenceControlFeatures |= eConferenceControlFeatures.AutoAnswer;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.DoNotDisturb;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.Hold;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CanDial;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CanEnd;

			Subscribe(m_Line);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Line);

			foreach (int item in m_AppearanceConferences.Keys.ToArray())
				RemoveConference(item);

			foreach (int item in m_AppearanceIncomingCalls.Keys.ToArray())
				RemoveIncomingCall(item);
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

			switch (dialContext.Protocol)
			{
				case eDialProtocol.Pstn:
					return eDialContextSupport.Supported;
				case eDialProtocol.Unknown:
					return eDialContextSupport.Unknown;
				default:
					return eDialContextSupport.Unsupported;
			}
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			m_AppearanceConferencesSection.Enter();

			try
			{
				// Find the first empty CallAppearance
				VoIpControlStatusCallAppearance callAppearance
					= m_Line.GetCallAppearances()
							.FirstOrDefault(c => !m_AppearanceConferences.ContainsKey(c.Index) 
								&& !m_AppearanceIncomingCalls.ContainsKey(c.Index));

				if (callAppearance == null)
				{
					Logger.Log(eSeverity.Error, "Unable to dial - could not find an unused call appearance");
					return;
				}

				m_LastDialedNumber = dialContext.DialString;
				callAppearance.Dial(dialContext.DialString);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			m_Line.SetDndEnabled(enabled);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			m_Line.SetAutoAnswer(enabled);
		}

		#endregion

		#region Private Methods

		private static eConferenceStatus VoIpCallStateToSourceStatus(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
					return eConferenceStatus.Undefined;

				case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
					return eConferenceStatus.Connecting;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
				case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
					return eConferenceStatus.Disconnected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eConferenceStatus.Connecting;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
					return eConferenceStatus.Connected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
					return eConferenceStatus.OnHold;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		/// <summary>
		/// Creates a source if a call is active but no source exists yet. Clears the source if an existing call becomes inactive.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		private void CreateOrRemoveConferenceForCallState(int index, VoIpControlStatusCallAppearance.eVoIpCallState state)
		{

			//eConferenceStatus status = VoIpCallStateToSourceStatus(state);

			m_AppearanceConferencesSection.Enter();

			try
			{
				ThinConference source = GetConferenceAtIndex(index);
				if (source != null)
				{
					VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
					UpdateConference(source, callAppearance);
				}

				IIncomingCall call = GetIncomingCall(index);
				if (call != null)
				{
					VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
					UpdateIncomingCall(call, callAppearance);
				}

				switch (state)
				{
					case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
					case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
						if (source != null)
							RemoveConference(index);
						RemoveIncomingCall(index);
						break;

					case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
						if (call == null && VoIpCallStateToDirection(state) == eCallDirection.Incoming)
							CreateIncomingCall(index);
						else if (source == null)
							CreateConference(index);
						break;

					case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
					case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
					case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
					case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
					case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
					case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
					case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
					case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
					case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
						if (source == null)
							CreateConference(index);
						break;
				}
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		/// <summary>
		/// Updates the source to match the state of the given call appearance.
		/// </summary>
		/// <param name="conference"></param>
		/// <param name="callAppearance"></param>
		private void UpdateConference(ThinConference conference, VoIpControlStatusCallAppearance callAppearance)
		{
			if (conference == null || callAppearance == null)
				return;

			eConferenceStatus status = VoIpCallStateToSourceStatus(callAppearance.State);

			if (!string.IsNullOrEmpty(callAppearance.CallerName))
				conference.Name = callAppearance.CallerName;

			if (!string.IsNullOrEmpty(callAppearance.CallerNumber))
				conference.Number = callAppearance.CallerNumber;

			conference.Status = status;
			conference.Name = conference.Name ?? conference.Number;

			// Assume the call is outgoing unless we discover otherwise.
			eCallDirection direction = VoIpCallStateToDirection(callAppearance.State);
			if (direction == eCallDirection.Incoming)
			{
				m_LastDialedNumber = null;
				conference.Direction = eCallDirection.Incoming;
			}
			else if (conference.Direction != eCallDirection.Incoming)
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

			// Start/End
			switch (status)
			{
				case eConferenceStatus.Connected:
					conference.StartTime = conference.StartTime ?? IcdEnvironment.GetUtcTime();
					if (conference.Direction == eCallDirection.Incoming && AutoAnswer)
						conference.AnswerState = eCallAnswerState.AutoAnswered;
					else
						conference.AnswerState = eCallAnswerState.Answered;
					break;
				case eConferenceStatus.Disconnected:
					conference.EndTime = conference.EndTime ?? IcdEnvironment.GetUtcTime();
					if (conference.AnswerState == eCallAnswerState.Unknown)
						conference.AnswerState = eCallAnswerState.Unanswered;
					break;
			}
		}

		[CanBeNull]
		private ThinConference GetConferenceAtIndex(int index)
		{
			return m_AppearanceConferencesSection.Execute(() => m_AppearanceConferences.GetDefault(index));
		}

		/// <summary>
		/// Creates a source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private void CreateConference(int index)
		{
			ThinConference conference;

			m_AppearanceConferencesSection.Enter();

			try
			{
				IIncomingCall call = GetIncomingCall(index);
				if (call != null)
					RemoveIncomingCall(index);

				RemoveConference(index);

				conference = new ThinConference
				{
					CallType = eCallType.Audio
				};

				Subscribe(conference);

				VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
				UpdateConference(conference, callAppearance);

				m_AppearanceConferences.Add(index, conference);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}

			AddConference(conference);
		}

		/// <summary>
		/// Removes the source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		private void RemoveConference(int index)
		{
			m_AppearanceConferencesSection.Enter();

			try
			{
				ThinConference source;
				if (!m_AppearanceConferences.TryGetValue(index, out source))
					return;

				Unsubscribe(source);

				RemoveConference(source);

				m_AppearanceConferences.Remove(index);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		/// <summary>
		/// Updates the source to match the state of the given call appearance.
		/// </summary>
		/// <param name="call"></param>
		/// <param name="callAppearance"></param>
		private void UpdateIncomingCall(IIncomingCall call, VoIpControlStatusCallAppearance callAppearance)
		{
			if (call == null || callAppearance == null)
				return;

			if (!string.IsNullOrEmpty(callAppearance.CallerName))
				call.Name = callAppearance.CallerName;

			if (!string.IsNullOrEmpty(callAppearance.CallerNumber))
				call.Number = callAppearance.CallerNumber;

			call.Name = call.Name ?? call.Number;
			
			m_LastDialedNumber = null;

			// Don't update the answer state if we can't determine the current answer state
			// Don't update the answer state if it's already set to a answered state
			eCallAnswerState answerState = VoIpCallStateToIncomingAnswerState(callAppearance.State);
			if ((call.AnswerState != eCallAnswerState.Unanswered || call.AnswerState == eCallAnswerState.Unknown) &&
				answerState != eCallAnswerState.Unknown)
				call.AnswerState = answerState;
		}

		[CanBeNull]
		private IIncomingCall GetIncomingCall(int index)
		{
			return m_AppearanceConferencesSection.Execute(() => m_AppearanceIncomingCalls.GetDefault(index));
		}

		/// <summary>
		/// Creates a source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private void CreateIncomingCall(int index)
		{
			TraditionalIncomingCall call;

			m_AppearanceConferencesSection.Enter();

			try
			{
				RemoveIncomingCall(index);

				ThinConference conference = GetConferenceAtIndex(index);
				if (conference != null)
					RemoveConference(index);

				call = new TraditionalIncomingCall(eCallType.Audio);

				Subscribe(call);

				VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
				UpdateIncomingCall(call, callAppearance);

				m_AppearanceIncomingCalls.Add(index, call);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}

			AddIncomingCall(call);
		}

		/// <summary>
		/// Removes the source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		private void RemoveIncomingCall(int index)
		{
			m_AppearanceConferencesSection.Enter();

			try
			{
				IIncomingCall call;
				if (!m_AppearanceIncomingCalls.TryGetValue(index, out call))
					return;

				//If no answer state, set to ignored
				if (call.AnswerState == eCallAnswerState.Unanswered || call.AnswerState == eCallAnswerState.Unknown)
					call.AnswerState = eCallAnswerState.Ignored;

				Unsubscribe(call);

				RemoveIncomingCall(call);

				m_AppearanceIncomingCalls.Remove(index);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		private static eCallDirection VoIpCallStateToDirection(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eCallDirection.Incoming;

				default:
					return eCallDirection.Undefined;
			}
		}

		private eCallAnswerState VoIpCallStateToIncomingAnswerState(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
				case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
					return eCallAnswerState.Unknown;

				case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
				case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
					return eCallAnswerState.Unanswered;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return AutoAnswer ? eCallAnswerState.AutoAnswered : eCallAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		#endregion

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source callbacks.
		/// </summary>
		/// <param name="conference"></param>
		private void Subscribe(ThinConference conference)
		{
			conference.HoldCallback += HoldCallback;
			conference.ResumeCallback += ResumeCallback;
			conference.SendDtmfCallback += SendDtmfCallback;
			conference.LeaveConferenceCallback += HangupCallback;
		}
           
		/// <summary>
		/// Unsubscribe from the source callbacks.
		/// </summary>
		/// <param name="conference"></param>
		private void Unsubscribe(ThinConference conference)
		{
			conference.HoldCallback = null;
			conference.ResumeCallback = null;
			conference.SendDtmfCallback = null;
			conference.LeaveConferenceCallback = null;
		}

		private void HoldCallback(ThinConference sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Hold();
		}

		private void ResumeCallback(ThinConference sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Resume();
		}

		private void SendDtmfCallback(ThinConference sender, string data)
		{
			foreach (char digit in data)
				m_Line.Dtmf(digit);
		}

		private void HangupCallback(ThinConference sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).End();
		}

		private bool TryGetCallAppearance(ThinConference source, out int index)
		{
			m_AppearanceConferencesSection.Enter();

			try
			{
				return m_AppearanceConferences.TryGetKey(source, out index);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe(IIncomingCall call)
		{
			TraditionalIncomingCall castCall = call as TraditionalIncomingCall;
			if(castCall == null)
				return;

			castCall.AnswerCallback += AnswerCallback;
			castCall.RejectCallback += RejectCallback;
		}

		private void Unsubscribe(IIncomingCall call)
		{
			TraditionalIncomingCall castCall = call as TraditionalIncomingCall;
			if (castCall == null)
				return;

			castCall.AnswerCallback = null;
			castCall.RejectCallback = null;
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			int index;
			if (!TryGetCallAppearance(sender, out index))
				return;

			m_Line.GetCallAppearance(index).Answer();
			sender.AnswerState = eCallAnswerState.Answered;
		}

		private void RejectCallback(IIncomingCall sender)
		{
			int index;
			if (!TryGetCallAppearance(sender, out index))
				return;

			m_Line.GetCallAppearance(index).End();
			sender.AnswerState = eCallAnswerState.Rejected;
		}

		private bool TryGetCallAppearance(IIncomingCall source, out int index)
		{
			m_AppearanceConferencesSection.Enter();

			try
			{
				return m_AppearanceIncomingCalls.TryGetKey(source, out index);
			}
			finally
			{
				m_AppearanceConferencesSection.Leave();
			}
		}

		#endregion

		#region Line Callbacks

		/// <summary>
		/// Subscribe to the line callbacks.
		/// </summary>
		/// <param name="line"></param>
		private void Subscribe(VoIpControlStatusLine line)
		{
			if (line == null)
				return;

			line.OnAutoAnswerChanged += LineOnAutoAnswerChanged;
			line.OnDndEnabledChanged += LineOnDndEnabledChanged;

			foreach (VoIpControlStatusCallAppearance appearance in line.GetCallAppearances())
				Subscribe(appearance);
		}

		/// <summary>
		/// Unsubscribe from the line callbacks.
		/// </summary>
		/// <param name="line"></param>
		private void Unsubscribe(VoIpControlStatusLine line)
		{
			if (line == null)
				return;

			line.OnAutoAnswerChanged -= LineOnAutoAnswerChanged;
			line.OnDndEnabledChanged -= LineOnDndEnabledChanged;

			foreach (VoIpControlStatusCallAppearance appearance in line.GetCallAppearances())
				Unsubscribe(appearance);
		}

		/// <summary>
		/// Called when the line auto-answer state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void LineOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			AutoAnswer = args.Data;
		}

		private void LineOnDndEnabledChanged(object sender, BoolEventArgs args)
		{
			DoNotDisturb = args.Data;
		}

		#endregion

		#region Call Appearance Callbacks

		/// <summary>
		/// Subscribe to the call appearance callbacks.
		/// </summary>
		/// <param name="appearance"></param>
		private void Subscribe(VoIpControlStatusCallAppearance appearance)
		{
			appearance.OnCallStateChanged += AppearanceOnCallStateChanged;
			appearance.OnCallerNumberChanged += AppearanceOnCallerNumberChanged;
			appearance.OnCallerNameChanged += AppearanceOnCallerNameChanged;
		}

		/// <summary>
		/// Unsubscribe from the call appearance callbacks.
		/// </summary>
		/// <param name="appearance"></param>
		private void Unsubscribe(VoIpControlStatusCallAppearance appearance)
		{
			appearance.OnCallStateChanged -= AppearanceOnCallStateChanged;
			appearance.OnCallerNumberChanged -= AppearanceOnCallerNumberChanged;
			appearance.OnCallerNameChanged -= AppearanceOnCallerNameChanged;
		}

		private void AppearanceOnCallerNumberChanged(object sender, StringEventArgs args)
		{
			VoIpControlStatusCallAppearance callAppearance = sender as VoIpControlStatusCallAppearance;
			if (callAppearance == null)
				return;

			ThinConference source = GetConferenceAtIndex(callAppearance.Index);
			if (source != null)
				UpdateConference(source, callAppearance);

			IIncomingCall call = GetIncomingCall(callAppearance.Index);
			if (call != null)
				UpdateIncomingCall(call, callAppearance);
		}

		private void AppearanceOnCallerNameChanged(object sender, StringEventArgs args)
		{
			VoIpControlStatusCallAppearance callAppearance = sender as VoIpControlStatusCallAppearance;
			if (callAppearance == null)
				return;

			ThinConference source = GetConferenceAtIndex(callAppearance.Index);
			if(source != null)
				UpdateConference(source, callAppearance);

			IIncomingCall call = GetIncomingCall(callAppearance.Index);
			if (call != null)
				UpdateIncomingCall(call, callAppearance);
		}

		/// <summary>
		/// Called when a call appearance state changes.
		/// </summary>
		/// <param name="callAppearance"></param>
		/// <param name="state"></param>
		private void AppearanceOnCallStateChanged(VoIpControlStatusCallAppearance callAppearance, VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			CreateOrRemoveConferenceForCallState(callAppearance.Index, state);
		}

		#endregion
	}
}
