using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing.VoIP
{
	public sealed class VoIpConferenceDeviceControl : AbstractBiampTesiraConferenceDeviceControl
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		private readonly VoIpControlStatusLine m_Line;

		private readonly Dictionary<int, ThinTraditionalParticipant> m_AppearanceSources;
		private readonly Dictionary<int, IIncomingCall> m_AppearanceIncomingCalls; 
		private readonly SafeCriticalSection m_AppearanceSourcesSection;
		private string m_LastDialedNumber;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="line"></param>
		/// <param name="privacyMuteControl"></param>
		public VoIpConferenceDeviceControl(int id, string name, VoIpControlStatusLine line,
		                                   IBiampTesiraStateDeviceControl privacyMuteControl)
			: base(id, name, line.Device, privacyMuteControl)
		{
			m_AppearanceSources = new Dictionary<int, ThinTraditionalParticipant>();
			m_AppearanceIncomingCalls = new Dictionary<int, IIncomingCall>();
			m_AppearanceSourcesSection = new SafeCriticalSection();

			m_Line = line;

			Subscribe(m_Line);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Line);

			foreach (int item in m_AppearanceSources.Keys.ToArray())
				RemoveSource(item);

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
			m_AppearanceSourcesSection.Enter();

			try
			{
				// Find the first empty CallAppearance
				VoIpControlStatusCallAppearance callAppearance
					= m_Line.GetCallAppearances()
							.FirstOrDefault(c => !m_AppearanceSources.ContainsKey(c.Index) 
								&& !m_AppearanceIncomingCalls.ContainsKey(c.Index));

				if (callAppearance == null)
				{
					Parent.Log(eSeverity.Error, "Unable to dial - could not find an unused call appearance");
					return;
				}

				m_LastDialedNumber = dialContext.DialString;
				callAppearance.Dial(dialContext.DialString);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
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

		/// <summary>
		/// Creates a source if a call is active but no source exists yet. Clears the source if an existing call becomes inactive.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		private void CreateOrRemoveSourceForCallState(int index, VoIpControlStatusCallAppearance.eVoIpCallState state)
		{

			eParticipantStatus status = VoIpCallStateToSourceStatus(state);

			m_AppearanceSourcesSection.Enter();

			try
			{
				ThinTraditionalParticipant source = GetSource(index);
				if (source != null)
				{
					VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
					UpdateSource(source, callAppearance);
				}

				IIncomingCall call = GetIncomingCall(index);
				if (call != null)
				{
					VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
					UpdateIncomingCall(call, callAppearance);
				}

				switch (status)
				{
					case eParticipantStatus.Dialing:
					case eParticipantStatus.Ringing:
						if (call == null && VoIpCallStateToDirection(state) == eCallDirection.Incoming)
							CreateIncomingCall(index);
						else if (source == null)
							CreateSource(index);
						break;

					case eParticipantStatus.Connecting:
					case eParticipantStatus.Connected:
					case eParticipantStatus.OnHold:
					case eParticipantStatus.EarlyMedia:
					case eParticipantStatus.Preserved:
					case eParticipantStatus.RemotePreserved:
						if (source == null)
							CreateSource(index);
						break;

					case eParticipantStatus.Undefined:
					case eParticipantStatus.Idle:
					case eParticipantStatus.Disconnecting:
					case eParticipantStatus.Disconnected:
						if (source != null)
							RemoveSource(index);
						break;
				}
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}
		}

		/// <summary>
		/// Updates the source to match the state of the given call appearance.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="callAppearance"></param>
		private void UpdateSource(ThinTraditionalParticipant source, VoIpControlStatusCallAppearance callAppearance)
		{
			if (source == null || callAppearance == null)
				return;

			eParticipantStatus status = VoIpCallStateToSourceStatus(callAppearance.State);

			if (!string.IsNullOrEmpty(callAppearance.CallerName))
				source.SetName(callAppearance.CallerName);

			if (!string.IsNullOrEmpty(callAppearance.CallerNumber))
				source.SetNumber(callAppearance.CallerNumber);

			source.SetStatus(status);
			source.SetName(source.Name ?? source.Number);

			// Assume the call is outgoing unless we discover otherwise.
			eCallDirection direction = VoIpCallStateToDirection(callAppearance.State);
			if (direction == eCallDirection.Incoming)
			{
				m_LastDialedNumber = null;
				source.SetDirection(eCallDirection.Incoming);
			}
			else if (source.Direction != eCallDirection.Incoming)
			{
				if (string.IsNullOrEmpty(source.Number) &&
				    string.IsNullOrEmpty(source.Name) &&
				    !string.IsNullOrEmpty(m_LastDialedNumber))
				{
					source.SetNumber(m_LastDialedNumber);
					source.SetName(m_LastDialedNumber);
					m_LastDialedNumber = null;
				}

				source.SetDirection(eCallDirection.Outgoing);
			}

			// Start/End
			switch (status)
			{
				case eParticipantStatus.Connected:
					source.SetStart(source.Start ?? IcdEnvironment.GetLocalTime());
					break;
				case eParticipantStatus.Disconnected:
					source.SetEnd(source.End ?? IcdEnvironment.GetLocalTime());
					break;
			}
		}

		[CanBeNull]
		private ThinTraditionalParticipant GetSource(int index)
		{
			return m_AppearanceSourcesSection.Execute(() => m_AppearanceSources.GetDefault(index));
		}

		/// <summary>
		/// Creates a source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private void CreateSource(int index)
		{
			ThinTraditionalParticipant source;

			m_AppearanceSourcesSection.Enter();

			try
			{
				IIncomingCall call = GetIncomingCall(index);
				if (call != null)
					RemoveIncomingCall(index);

				RemoveSource(index);

				source = new ThinTraditionalParticipant();
				source.SetCallType(eCallType.Audio);

				Subscribe(source);

				VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
				UpdateSource(source, callAppearance);

				m_AppearanceSources.Add(index, source);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}

			AddParticipant(source);
		}

		/// <summary>
		/// Removes the source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		private void RemoveSource(int index)
		{
			m_AppearanceSourcesSection.Enter();

			try
			{
				ThinTraditionalParticipant source;
				if (!m_AppearanceSources.TryGetValue(index, out source))
					return;

				Unsubscribe(source);

				RemoveParticipant(source);

				m_AppearanceSources.Remove(index);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
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

			// Assume the call is outgoing unless we discover otherwise.
			eCallDirection direction = VoIpCallStateToDirection(callAppearance.State);
			if (direction == eCallDirection.Incoming)
			{
				m_LastDialedNumber = null;
				call.Direction = eCallDirection.Incoming;
			}
			else if (call.Direction != eCallDirection.Incoming)
			{
				if (string.IsNullOrEmpty(call.Number) &&
					string.IsNullOrEmpty(call.Name) &&
					!string.IsNullOrEmpty(m_LastDialedNumber))
				{
					call.Number = m_LastDialedNumber;
					call.Name = m_LastDialedNumber;
					m_LastDialedNumber = null;
				}

				call.Direction = eCallDirection.Outgoing;
			}

			// Don't update the answer state if we can't determine the current answer state
			eCallAnswerState answerState = VoIpCallStateToAnswerState(callAppearance.State);
			if (answerState != eCallAnswerState.Unknown)
				call.AnswerState = answerState;
		}

		[CanBeNull]
		private IIncomingCall GetIncomingCall(int index)
		{
			return m_AppearanceSourcesSection.Execute(() => m_AppearanceIncomingCalls.GetDefault(index));
		}

		/// <summary>
		/// Creates a source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private void CreateIncomingCall(int index)
		{
			TraditionalIncomingCall call;

			m_AppearanceSourcesSection.Enter();

			try
			{
				RemoveIncomingCall(index);

				ThinTraditionalParticipant source = GetSource(index);
				if (source != null)
					RemoveSource(index);

				call = new TraditionalIncomingCall(eCallType.Audio);

				Subscribe(call);

				VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
				UpdateIncomingCall(call, callAppearance);

				m_AppearanceIncomingCalls.Add(index, call);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(call));
		}

		/// <summary>
		/// Removes the source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		private void RemoveIncomingCall(int index)
		{
			m_AppearanceSourcesSection.Enter();

			try
			{
				IIncomingCall call;
				if (!m_AppearanceIncomingCalls.TryGetValue(index, out call))
					return;

				Unsubscribe(call);

				OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(call));

				m_AppearanceIncomingCalls.Remove(index);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}
		}

		private static eParticipantStatus VoIpCallStateToSourceStatus(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
					return eParticipantStatus.Undefined;

				case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
					return eParticipantStatus.Dialing;

				case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
					return eParticipantStatus.Ringing;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
				case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
					return eParticipantStatus.Disconnected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eParticipantStatus.Connecting;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
					return eParticipantStatus.Connected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
					return eParticipantStatus.OnHold;

				default:
					throw new ArgumentOutOfRangeException("state");
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

		private static eCallAnswerState VoIpCallStateToAnswerState(VoIpControlStatusCallAppearance.eVoIpCallState state)
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
					return eCallAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		#endregion

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source callbacks.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(ThinTraditionalParticipant source)
		{
			source.HoldCallback += HoldCallback;
			source.ResumeCallback += ResumeCallback;
			source.SendDtmfCallback += SendDtmfCallback;
			source.HangupCallback += HangupCallback;
		}

		/// <summary>
		/// Unsubscribe from the source callbacks.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(ThinTraditionalParticipant source)
		{
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		}

		private void HoldCallback(ThinTraditionalParticipant sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Hold();
		}

		private void ResumeCallback(ThinTraditionalParticipant sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Resume();
		}

		private void SendDtmfCallback(ThinTraditionalParticipant sender, string data)
		{
			foreach (char digit in data)
				m_Line.Dtmf(digit);
		}

		private void HangupCallback(ThinTraditionalParticipant sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).End();
		}

		private bool TryGetCallAppearance(ThinTraditionalParticipant source, out int index)
		{
			m_AppearanceSourcesSection.Enter();

			try
			{
				return m_AppearanceSources.TryGetKey(source, out index);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
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
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Answer();
		}

		private void RejectCallback(IIncomingCall sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).End();
		}

		private bool TryGetCallAppearance(IIncomingCall source, out int index)
		{
			m_AppearanceSourcesSection.Enter();

			try
			{
				return m_AppearanceIncomingCalls.TryGetKey(source, out index);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
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

			ThinTraditionalParticipant source = GetSource(callAppearance.Index);
			if (source != null)
				UpdateSource(source, callAppearance);

			IIncomingCall call = GetIncomingCall(callAppearance.Index);
			if (call != null)
				UpdateIncomingCall(call, callAppearance);
		}

		private void AppearanceOnCallerNameChanged(object sender, StringEventArgs args)
		{
			VoIpControlStatusCallAppearance callAppearance = sender as VoIpControlStatusCallAppearance;
			if (callAppearance == null)
				return;

			ThinTraditionalParticipant source = GetSource(callAppearance.Index);
			if(source != null)
				UpdateSource(source, callAppearance);

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
			CreateOrRemoveSourceForCallState(callAppearance.Index, state);
		}

		#endregion
	}
}
