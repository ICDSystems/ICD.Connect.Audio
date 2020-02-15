﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing.Telephone
{
	public sealed class TiDialingDeviceControl : AbstractBiampTesiraDialingDeviceControl
	{
		/// <summary>
		/// Raised when the hold state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnHoldChanged;

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;


		private readonly TiControlStatusBlock m_TiControl;

		[CanBeNull]
		private readonly IBiampTesiraStateDeviceControl m_HoldControl;

		[CanBeNull]
		private readonly IBiampTesiraStateDeviceControl m_DoNotDisturbControl;

		private bool m_Hold;

		private ThinConferenceSource m_ActiveSource;
		private readonly SafeCriticalSection m_ActiveSourceSection;
		private string m_LastDialedNumber;

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

				Log(eSeverity.Informational, "{0} hold state set to {1}", this, m_Hold);

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
		public TiDialingDeviceControl(int id, string name, TiControlStatusBlock tiControl,
									  IBiampTesiraStateDeviceControl doNotDisturbControl,
									  IBiampTesiraStateDeviceControl privacyMuteControl,
									  IBiampTesiraStateDeviceControl holdControl)
			: base(id, name, tiControl.Device, privacyMuteControl)
		{
			m_ActiveSourceSection = new SafeCriticalSection();

			m_TiControl = tiControl;
			m_HoldControl = holdControl;
			m_DoNotDisturbControl = doNotDisturbControl;

			
			Subscribe(m_TiControl);
			Subscribe(m_HoldControl);
			SubscribeDoNotDisturb(m_DoNotDisturbControl);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			m_ActiveSourceSection.Enter();

			try
			{
				return m_ActiveSource == null
					       ? Enumerable.Empty<IConferenceSource>()
					       : new IConferenceSource[] {m_ActiveSource};
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			m_LastDialedNumber = number;
			m_TiControl.Dial(number);
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		/// <returns></returns>
		public override eBookingSupport CanDial(IBookingNumber bookingNumber)
		{
			var potsBooking = bookingNumber as IPstnBookingNumber;
			if (potsBooking != null && !string.IsNullOrEmpty(potsBooking.PhoneNumber))
				return eBookingSupport.Supported;

			return eBookingSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		public override void Dial(IBookingNumber bookingNumber)
		{
			var potsBooking = bookingNumber as IPstnBookingNumber;
			if (potsBooking == null || string.IsNullOrEmpty(potsBooking.PhoneNumber))
				return;

			Dial(potsBooking.PhoneNumber);
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
				Parent.Log(eSeverity.Error, "{0} unable to set Do-Not-Disturb - control is null", Name);
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
			OnSourceAdded = null;
			OnHoldChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_TiControl);
			Unsubscribe(m_HoldControl);
			UnsubscribeDoNotDisturb(m_DoNotDisturbControl);

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
		private void UpdateSource(ThinConferenceSource source)
		{
			if (source == null)
				return;

			eConferenceSourceStatus status = TiControlStateToSourceStatus(m_TiControl.State);
			if (IsOnline(status) && IsOnHold)
				status = eConferenceSourceStatus.OnHold;

			if (!string.IsNullOrEmpty(m_TiControl.CallerName))
				source.Name = m_TiControl.CallerName;

			if (!string.IsNullOrEmpty(m_TiControl.CallerNumber))
				source.Number = m_TiControl.CallerNumber;

			source.Status = status;
			source.Name = source.Name ?? source.Number;

			// Assume the call is outgoing unless we discover otherwise.
			eConferenceSourceDirection direction = TiControlStateToDirection(m_TiControl.State);
			if (direction == eConferenceSourceDirection.Incoming)
			{
				m_LastDialedNumber = null;
				source.Direction = eConferenceSourceDirection.Incoming;
			}
			else if (source.Direction != eConferenceSourceDirection.Incoming)
			{
				if (string.IsNullOrEmpty(source.Number) &&
					string.IsNullOrEmpty(source.Name) &&
					!string.IsNullOrEmpty(m_LastDialedNumber))
				{
					source.Number = m_LastDialedNumber;
					source.Name = m_LastDialedNumber;
					m_LastDialedNumber = null;
				}

				source.Direction = eConferenceSourceDirection.Outgoing;
			}

			// Don't update the answer state if we can't determine the current answer state
			eConferenceSourceAnswerState answerState = TiControlStateToAnswerState(m_TiControl.State);
			if (answerState != eConferenceSourceAnswerState.Unknown)
				source.AnswerState = answerState;

			// Start/End
			switch (status)
			{
				case eConferenceSourceStatus.Connected:
					source.Start = source.Start ?? IcdEnvironment.GetLocalTime();
					break;
				case eConferenceSourceStatus.Disconnected:
					source.End = source.End ?? IcdEnvironment.GetLocalTime();
					break;
			}
		}

		private static eConferenceSourceDirection TiControlStateToDirection(TiControlStatusBlock.eTiCallState state)
		{
			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eConferenceSourceDirection.Incoming;

				default:
					return eConferenceSourceDirection.Undefined;
			}
		}

		private static eConferenceSourceAnswerState TiControlStateToAnswerState(TiControlStatusBlock.eTiCallState state)
		{

			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.Idle:
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
					return eConferenceSourceAnswerState.Unknown;

				case TiControlStatusBlock.eTiCallState.Dialing:
				case TiControlStatusBlock.eTiCallState.RingBack:
				case TiControlStatusBlock.eTiCallState.Ringing:
					return eConferenceSourceAnswerState.Unanswered;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
					return eConferenceSourceAnswerState.Answered;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		private static eConferenceSourceStatus TiControlStateToSourceStatus(TiControlStatusBlock.eTiCallState state)
		{
			switch (state)
			{
				case TiControlStatusBlock.eTiCallState.Init:
				case TiControlStatusBlock.eTiCallState.Fault:
				case TiControlStatusBlock.eTiCallState.ErrorTone:
					return eConferenceSourceStatus.Undefined;

				case TiControlStatusBlock.eTiCallState.Dialing:
					return eConferenceSourceStatus.Dialing;

				case TiControlStatusBlock.eTiCallState.Ringing:
					return eConferenceSourceStatus.Ringing;

				case TiControlStatusBlock.eTiCallState.BusyTone:
				case TiControlStatusBlock.eTiCallState.Dropped:
				case TiControlStatusBlock.eTiCallState.Idle:
					return eConferenceSourceStatus.Disconnected;

				case TiControlStatusBlock.eTiCallState.Connected:
				case TiControlStatusBlock.eTiCallState.ConnectedMuted:
				case TiControlStatusBlock.eTiCallState.RingBack:
					return eConferenceSourceStatus.Connected;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		/// <summary>
		/// TODO - This belongs in conferencing utils somewhere.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		private static bool IsOnline(eConferenceSourceStatus status)
		{
			switch (status)
			{
				case eConferenceSourceStatus.Undefined:
				case eConferenceSourceStatus.Dialing:
				case eConferenceSourceStatus.Connecting:
				case eConferenceSourceStatus.Ringing:
				case eConferenceSourceStatus.Disconnecting:
				case eConferenceSourceStatus.Disconnected:
				case eConferenceSourceStatus.Idle:
					return false;

				case eConferenceSourceStatus.Connected:
				case eConferenceSourceStatus.OnHold:
				case eConferenceSourceStatus.EarlyMedia:
				case eConferenceSourceStatus.Preserved:
				case eConferenceSourceStatus.RemotePreserved:
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
			eConferenceSourceStatus status = TiControlStateToSourceStatus(state);

			m_ActiveSourceSection.Enter();

			try
			{
				if (m_ActiveSource != null)
					UpdateSource(m_ActiveSource);

				switch (status)
				{
					case eConferenceSourceStatus.Dialing:
					case eConferenceSourceStatus.Ringing:
					case eConferenceSourceStatus.Connecting:
					case eConferenceSourceStatus.Connected:
					case eConferenceSourceStatus.OnHold:
					case eConferenceSourceStatus.EarlyMedia:
					case eConferenceSourceStatus.Preserved:
					case eConferenceSourceStatus.RemotePreserved:
						if (m_ActiveSource == null)
							CreateActiveSource();
						break;

					case eConferenceSourceStatus.Undefined:
					case eConferenceSourceStatus.Idle:
					case eConferenceSourceStatus.Disconnecting:
					case eConferenceSourceStatus.Disconnected:
						if (m_ActiveSource != null)
							ClearCurrentSource();
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
				ClearCurrentSource();

				m_ActiveSource = new ThinConferenceSource { SourceType = eConferenceSourceType.Audio };
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

			SourceSubscribe(m_ActiveSource);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(m_ActiveSource));
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

				SourceUnsubscribe(m_ActiveSource);
				OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(m_ActiveSource));

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
		private void Subscribe(ThinConferenceSource source)
		{
			source.AnswerCallback += AnswerCallback;
			source.RejectCallback += RejectCallback;
			source.HoldCallback += HoldCallback;
			source.ResumeCallback += ResumeCallback;
			source.SendDtmfCallback += SendDtmfCallback;
			source.HangupCallback += HangupCallback;
		}

		/// <summary>
		/// Unsubscribe from the source callbacks.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(ThinConferenceSource source)
		{
			source.AnswerCallback = null;
			source.RejectCallback = null;
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		}

		private void AnswerCallback(ThinConferenceSource sender)
		{
			m_TiControl.Answer();
		}

		private void HoldCallback(ThinConferenceSource sender)
		{
			SetHold(true);
		}

		private void ResumeCallback(ThinConferenceSource sender)
		{
			SetHold(false);
		}

		private void SendDtmfCallback(ThinConferenceSource sender, string data)
		{
			foreach (char digit in data)
				m_TiControl.Dtmf(digit);
		}

		private void HangupCallback(ThinConferenceSource sender)
		{
			// Ends the active call.
			m_TiControl.End();
		}

		private void RejectCallback(ThinConferenceSource sender)
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

			holdControl.OnStateChanged += HoldControlOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void Unsubscribe(IBiampTesiraStateDeviceControl holdControl)
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

			UpdateSource(m_ActiveSource);
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
