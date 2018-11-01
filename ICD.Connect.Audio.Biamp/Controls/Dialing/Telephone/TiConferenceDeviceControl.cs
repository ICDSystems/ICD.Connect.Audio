using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing.Telephone
{
	public sealed class TiConferenceDeviceControl : AbstractBiampTesiraConferenceDeviceControl
	{
		/// <summary>
		/// Raised when the hold state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnHoldChanged;

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		private readonly TiControlStatusBlock m_TiControl;
		private readonly IBiampTesiraStateDeviceControl m_HoldControl;

		private bool m_Hold;

		private ThinTraditionalParticipant m_ActiveSource;
		private readonly SafeCriticalSection m_ActiveSourceSection;
		private string m_LastDialedNumber;

		private ThinIncomingCall m_IncomingCall;

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

				Logger.AddEntry(eSeverity.Informational, "{0} hold state set to {1}", this, m_Hold);

				OnHoldChanged.Raise(this, new BoolEventArgs(m_Hold));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="tiControl"></param>
		/// <param name="doNotDisturbControl"></param>
		/// <param name="privacyMuteControl"></param>
		/// <param name="holdControl"></param>
		public TiConferenceDeviceControl(int id, string name, TiControlStatusBlock tiControl,
									  IBiampTesiraStateDeviceControl doNotDisturbControl,
									  IBiampTesiraStateDeviceControl privacyMuteControl,
									  IBiampTesiraStateDeviceControl holdControl)
			: base(id, name, tiControl.Device, doNotDisturbControl, privacyMuteControl)
		{
			m_ActiveSourceSection = new SafeCriticalSection();

			m_TiControl = tiControl;
			m_HoldControl = holdControl;

			Subscribe(m_TiControl);
			Subscribe(m_HoldControl);
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
			Unsubscribe(m_HoldControl);

			ClearCurrentSource();
		}

		/// <summary>
		/// Sets the hold state.
		/// </summary>
		/// <param name="hold"></param>
		private void SetHold(bool hold)
		{
			if (m_HoldControl == null)
			{
				Parent.Log(eSeverity.Error, "{0} unable to hold - control is null", Name);
				return;
			}

			m_HoldControl.SetState(hold);
		}

		/// <summary>
		/// Updates the source to match the state of the TI block.
		/// </summary>
		/// <param name="source"></param>
		private void UpdateSource(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			eParticipantStatus status = TiControlStateToSourceStatus(m_TiControl.State);
			if (IsOnline(status) && IsOnHold)
				status = eParticipantStatus.OnHold;

			if (!string.IsNullOrEmpty(m_TiControl.CallerName))
				source.Name = m_TiControl.CallerName;

			if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				source.Number = m_TiControl.CallerNumber;

			source.Status = status;
			source.Name = source.Name ?? source.Number;

			if (source.Direction != eCallDirection.Incoming)
			{
				if (string.IsNullOrEmpty(source.Number) &&
					string.IsNullOrEmpty(source.Name) &&
					!string.IsNullOrEmpty(m_LastDialedNumber))
				{
					source.Number = m_LastDialedNumber;
					source.Name = m_LastDialedNumber;
					m_LastDialedNumber = null;
				}

				source.Direction = eCallDirection.Outgoing;
			}

			// Start/End
			switch (status)
			{
				case eParticipantStatus.Connected:
					source.Start = source.Start ?? IcdEnvironment.GetLocalTime();
					break;
				case eParticipantStatus.Disconnected:
					source.End = source.End ?? IcdEnvironment.GetLocalTime();
					break;
			}
		}

		private void UpdateIncomingCall(ThinIncomingCall call)
		{
			if (!string.IsNullOrEmpty(m_TiControl.CallerName))
				call.Name = m_TiControl.CallerName;

			if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				call.Number = m_TiControl.CallerNumber;

			call.Name = call.Name ?? call.Number;

			// Don't update the answer state if we can't determine the current answer state
			eCallAnswerState answerState = TiControlStateToAnswerState(m_TiControl.State);
			if (answerState != eCallAnswerState.Unknown)
				call.AnswerState = answerState;

			// Assume the call is outgoing unless we discover otherwise.
			eCallDirection direction = TiControlStateToDirection(m_TiControl.State);
			if (direction == eCallDirection.Incoming)
			{
				m_LastDialedNumber = null;
				call.Direction = eCallDirection.Incoming;
			}
		}

		private static eCallDirection TiControlStateToDirection(TiControlStatusBlock.eTiCallState state)
		{
			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eCallDirection.Incoming;

				default:
					return eCallDirection.Undefined;
			}
		}

		private static eCallAnswerState TiControlStateToAnswerState(TiControlStatusBlock.eTiCallState state)
		{

			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.Idle:
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
					return eCallAnswerState.Unknown;

				case TiControlStatusBlock.eTiCallState.Dialing:
				case TiControlStatusBlock.eTiCallState.RingBack:
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eCallAnswerState.Unanswered;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
					return eCallAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		private static eParticipantStatus TiControlStateToSourceStatus(TiControlStatusBlock.eTiCallState state)
		{
			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
					return eParticipantStatus.Undefined;

				case TiControlStatusBlock.eTiCallState.Dialing:
					return eParticipantStatus.Dialing;

				case TiControlStatusBlock.eTiCallState.RingBack:
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eParticipantStatus.Ringing;

				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
				case TiControlStatusBlock.eTiCallState.Idle:
					return eParticipantStatus.Disconnected;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
					return eParticipantStatus.Connected;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		/// <summary>
		/// TODO - This belongs in conferencing utils somewhere.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		private static bool IsOnline(eParticipantStatus status)
		{
			switch (status)
			{
				case eParticipantStatus.Undefined:
				case eParticipantStatus.Dialing:
				case eParticipantStatus.Connecting:
				case eParticipantStatus.Ringing:
				case eParticipantStatus.Disconnecting:
				case eParticipantStatus.Disconnected:
				case eParticipantStatus.Idle:
					return false;

				case eParticipantStatus.Connected:
				case eParticipantStatus.OnHold:
				case eParticipantStatus.EarlyMedia:
				case eParticipantStatus.Preserved:
				case eParticipantStatus.RemotePreserved:
					return true;

				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		#endregion

		#region Sources

		/// <summary>
		/// Creates a source if a call is active but no source exists yet. Clears the source if an existing call becomes inactive.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		private void CreateOrRemoveSourceForCallState(TiControlStatusBlock.eTiCallState state)
		{
			eParticipantStatus status = TiControlStateToSourceStatus(state);

			m_ActiveSourceSection.Enter();

			try
			{

				switch (status)
				{
					case eParticipantStatus.Ringing:
						if (m_IncomingCall == null)
							CreateIncomingCall();
						else
							UpdateIncomingCall(m_IncomingCall);
						break;

					case eParticipantStatus.Dialing:
					case eParticipantStatus.Connecting:
					case eParticipantStatus.Connected:
					case eParticipantStatus.OnHold:
					case eParticipantStatus.EarlyMedia:
					case eParticipantStatus.Preserved:
					case eParticipantStatus.RemotePreserved:
						if (m_ActiveSource == null)
							CreateActiveSource();
						else
							UpdateSource(m_ActiveSource);
						break;

					case eParticipantStatus.Undefined:
					case eParticipantStatus.Idle:
					case eParticipantStatus.Disconnecting:
					case eParticipantStatus.Disconnected:
						if (m_ActiveSource != null)
							ClearCurrentSource();
						if (m_IncomingCall != null)
							ClearCurrentIncomingCall();
						break;
				}
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates a new active source.
		/// </summary>
		private void CreateActiveSource()
		{
			m_ActiveSourceSection.Enter();

			try
			{
				if(m_IncomingCall != null)
					ClearCurrentIncomingCall();

				ClearCurrentSource();

				m_ActiveSource = new ThinTraditionalParticipant { SourceType = eCallType.Audio };
				Subscribe(m_ActiveSource);

				// Setup the source properties
				UpdateSource(m_ActiveSource);

				// Clear the hold state between calls
				SetHold(false);
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}

			AddParticipant(m_ActiveSource);
		}

		/// <summary>
		/// Unsubscribes from the current source and clears the field.
		/// </summary>
		private void ClearCurrentSource()
		{
			m_ActiveSourceSection.Enter();

			try
			{
				if (m_ActiveSource == null)
					return;

				UpdateSource(m_ActiveSource);

				Unsubscribe(m_ActiveSource);

				RemoveParticipant(m_ActiveSource);

				m_ActiveSource = null;

				// Clear the hold state between calls
				SetHold(false);
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

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
			SetHold(true);
		}

		private void ResumeCallback(ThinTraditionalParticipant sender)
		{
			SetHold(false);
		}

		private void SendDtmfCallback(ThinTraditionalParticipant sender, string data)
		{
			foreach (char digit in data)
				m_TiControl.Dtmf(digit);
		}

		private void HangupCallback(ThinTraditionalParticipant sender)
		{
			// Ends the active call.
			m_TiControl.End();
		}

		#endregion

		#region Incoming Calls

		private void CreateIncomingCall()
		{
			m_ActiveSourceSection.Enter();
			try
			{
				if (m_ActiveSource != null)
					ClearCurrentSource();

				ClearCurrentIncomingCall();

				m_IncomingCall = new ThinIncomingCall();
				Subscribe(m_IncomingCall);
				UpdateIncomingCall(m_IncomingCall);

				SetHold(false);
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(m_IncomingCall));
		}

		private void ClearCurrentIncomingCall()
		{
			m_ActiveSourceSection.Enter();
			try
			{
				if (m_IncomingCall == null)
					return;

				UpdateIncomingCall(m_IncomingCall);
				Unsubscribe(m_IncomingCall);
				OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(m_IncomingCall));
				m_IncomingCall = null;

				SetHold(false);
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

		private void Subscribe(ThinIncomingCall call)
		{
			call.AnswerCallback += AnswerCallback;
			call.RejectCallback += RejectCallback;
		}

		private void Unsubscribe(ThinIncomingCall call)
		{
			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void AnswerCallback(ThinIncomingCall sender)
		{
			m_TiControl.Answer();
		}

		private void RejectCallback(ThinIncomingCall sender)
		{
			// Rejects the incoming call.
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
		private void Subscribe(IBiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			m_HoldControl.OnStateChanged += HoldControlOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void Unsubscribe(IBiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			m_HoldControl.OnStateChanged -= HoldControlOnStateChanged;
		}

		/// <summary>
		/// Called when the hold control state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void HoldControlOnStateChanged(object sender, BoolEventArgs args)
		{
			IsOnHold = m_HoldControl.State;

			UpdateSource(m_ActiveSource);
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
			if (m_ActiveSource != null)
				UpdateSource(m_ActiveSource);
		}

		private void AttributeInterfaceOnCallerNameChanged(object sender, StringEventArgs stringEventArgs)
		{
			if (m_ActiveSource != null)
				UpdateSource(m_ActiveSource);
		}

		private void AttributeInterfaceOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			AutoAnswer = args.Data;
		}

		private void AttributeInterfaceOnCallStateChanged(TiControlStatusBlock sender, TiControlStatusBlock.eTiCallState callState)
		{
			CreateOrRemoveSourceForCallState(callState);
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
