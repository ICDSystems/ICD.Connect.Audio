using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing.VoIP
{
	public sealed class VoIpDialingDeviceControl : AbstractBiampTesiraDialingDeviceControl
	{
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly VoIpControlStatusLine m_Line;

		private readonly Dictionary<int, ThinConferenceSource> m_AppearanceSources;
		private readonly SafeCriticalSection m_AppearanceSourcesSection;
		private string m_LastDialedNumber;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="line"></param>
		/// <param name="doNotDisturbControl"></param>
		/// <param name="privacyMuteControl"></param>
		public VoIpDialingDeviceControl(int id, string name, VoIpControlStatusLine line,
										IBiampTesiraStateDeviceControl doNotDisturbControl,
										IBiampTesiraStateDeviceControl privacyMuteControl)
			: base(id, name, line.Device, doNotDisturbControl, privacyMuteControl)
		{
			m_AppearanceSources = new Dictionary<int, ThinConferenceSource>();
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
			OnSourceAdded = null;
			OnSourceRemoved = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Line);

			foreach (int item in m_AppearanceSources.Keys.ToArray())
				RemoveSource(item);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			return m_AppearanceSourcesSection.Execute(() => m_AppearanceSources.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			m_AppearanceSourcesSection.Enter();

			try
			{
				// Find the first empty CallAppearance
				VoIpControlStatusCallAppearance callAppearance
					= m_Line.GetCallAppearances()
					        .FirstOrDefault(c => !m_AppearanceSources.ContainsKey(c.Index));

				if (callAppearance == null)
				{
					Parent.Log(eSeverity.Error, "Unable to dial - could not find an unused call appearance");
					return;
				}

				m_LastDialedNumber = number;
				callAppearance.Dial(number);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}
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
		/// Updates the source to match the state of the given call appearance.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="callAppearance"></param>
		private void UpdateSource(ThinConferenceSource source, VoIpControlStatusCallAppearance callAppearance)
		{
			if (source == null || callAppearance == null)
				return;

			eConferenceSourceStatus status = VoIpCallStateToSourceStatus(callAppearance.State);

			if (!string.IsNullOrEmpty(callAppearance.CallerName))
				source.Name = callAppearance.CallerName;

			if (!string.IsNullOrEmpty(callAppearance.CallerNumber))
				source.Number = callAppearance.CallerNumber;

			source.Status = status;
			source.Name = source.Name ?? source.Number;

			// Assume the call is outgoing unless we discover otherwise.
			eConferenceSourceDirection direction = VoIpCallStateToDirection(callAppearance.State);
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
			eConferenceSourceAnswerState answerState = VoIpCallStateToAnswerState(callAppearance.State);
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

		/// <summary>
		/// Creates a source if a call is active but no source exists yet. Clears the source if an existing call becomes inactive.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		private ThinConferenceSource CreateOrRemoveSourceForCallState(int index,
		                                                                VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			eConferenceSourceStatus status = VoIpCallStateToSourceStatus(state);

			m_AppearanceSourcesSection.Enter();

			ThinConferenceSource source = GetSource(index);

			try
			{
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
						if (source == null)
							return CreateSource(index);
						break;

					case eConferenceSourceStatus.Undefined:
					case eConferenceSourceStatus.Idle:
					case eConferenceSourceStatus.Disconnecting:
					case eConferenceSourceStatus.Disconnected:
						if (source != null)
							RemoveSource(index);
						return source;
				}
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}

			return source;
		}

		[CanBeNull]
		private ThinConferenceSource GetSource(int index)
		{
			return m_AppearanceSourcesSection.Execute(() => m_AppearanceSources.GetDefault(index));
		}

		/// <summary>
		/// Creates a source for the given call appearance index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private ThinConferenceSource CreateSource(int index)
		{
			ThinConferenceSource source;

			m_AppearanceSourcesSection.Enter();

			RemoveSource(index);

			try
			{
				source = new ThinConferenceSource();

				Subscribe(source);

				VoIpControlStatusCallAppearance callAppearance = m_Line.GetCallAppearance(index);
				UpdateSource(source, callAppearance);

				m_AppearanceSources.Add(index, source);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}

			SourceSubscribe(source);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
			return source;
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
				ThinConferenceSource source;
				if (!m_AppearanceSources.TryGetValue(index, out source))
					return;

				Unsubscribe(source);

				SourceUnsubscribe(source);
				OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(source));

				m_AppearanceSources.Remove(index);
			}
			finally
			{
				m_AppearanceSourcesSection.Leave();
			}
		}

		private static eConferenceSourceStatus VoIpCallStateToSourceStatus(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
					return eConferenceSourceStatus.Undefined;

				case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
					return eConferenceSourceStatus.Dialing;

				case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
					return eConferenceSourceStatus.Ringing;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
				case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
					return eConferenceSourceStatus.Disconnected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eConferenceSourceStatus.Connecting;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
					return eConferenceSourceStatus.Connected;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
					return eConferenceSourceStatus.OnHold;

				default:
					throw new ArgumentOutOfRangeException("state");
			}
		}

		private static eConferenceSourceDirection VoIpCallStateToDirection(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eConferenceSourceDirection.Incoming;

				default:
					return eConferenceSourceDirection.Undefined;
			}
		}

		private static eConferenceSourceAnswerState VoIpCallStateToAnswerState(VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			switch (state)
			{
				case VoIpControlStatusCallAppearance.eVoIpCallState.Fault:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Idle:
				case VoIpControlStatusCallAppearance.eVoIpCallState.DialTone:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Silent:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Init:
					return eConferenceSourceAnswerState.Unknown;

				case VoIpControlStatusCallAppearance.eVoIpCallState.WaitingRing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Dialing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.RingBack:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Ringing:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Busy:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Reject:
				case VoIpControlStatusCallAppearance.eVoIpCallState.InvalidNumber:
					return eConferenceSourceAnswerState.Unanswered;

				case VoIpControlStatusCallAppearance.eVoIpCallState.Hold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.Active:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ActiveMuted:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfActive:
				case VoIpControlStatusCallAppearance.eVoIpCallState.ConfHold:
				case VoIpControlStatusCallAppearance.eVoIpCallState.AnswerCall:
					return eConferenceSourceAnswerState.Answered;

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
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Answer();
		}

		private void HoldCallback(ThinConferenceSource sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Hold();
		}

		private void RejectCallback(ThinConferenceSource sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).End();
		}

		private void ResumeCallback(ThinConferenceSource sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).Resume();
		}

		private void SendDtmfCallback(ThinConferenceSource sender, string data)
		{
			foreach (char digit in data)
				m_Line.Dtmf(digit);
		}

		private void HangupCallback(ThinConferenceSource sender)
		{
			int index;
			if (TryGetCallAppearance(sender, out index))
				m_Line.GetCallAppearance(index).End();
		}

		private bool TryGetCallAppearance(ThinConferenceSource source, out int index)
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

			ThinConferenceSource source = GetSource(callAppearance.Index);
			UpdateSource(source, callAppearance);
		}

		private void AppearanceOnCallerNameChanged(object sender, StringEventArgs args)
		{
			VoIpControlStatusCallAppearance callAppearance = sender as VoIpControlStatusCallAppearance;
			if (callAppearance == null)
				return;

			ThinConferenceSource source = GetSource(callAppearance.Index);
			UpdateSource(source, callAppearance);
		}

		/// <summary>
		/// Called when a call appearance state changes.
		/// </summary>
		/// <param name="callAppearance"></param>
		/// <param name="state"></param>
		private void AppearanceOnCallStateChanged(VoIpControlStatusCallAppearance callAppearance, VoIpControlStatusCallAppearance.eVoIpCallState state)
		{
			ThinConferenceSource source = CreateOrRemoveSourceForCallState(callAppearance.Index, state);
			UpdateSource(source, callAppearance);
		}

		#endregion
	}
}
