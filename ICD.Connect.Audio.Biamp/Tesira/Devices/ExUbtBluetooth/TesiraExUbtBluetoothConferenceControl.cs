using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.ExUbt;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices.ExUbtBluetooth
{
	public sealed class TesiraExUbtBluetoothConferenceControl : AbstractConferenceDeviceControl<TesiraExUbtBluetoothDevice, TraditionalConference>
	{
		#region Events

		/// <summary>
		/// Raised when an incoming call is added to the dialing control.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when an incoming call is removed from the dialing control.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a conference is added to the dialing control.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a conference is removed from the dialing control.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		private ExUbtBluetoothControlStatusBlock m_Block;

		[CanBeNull]
		private TraditionalConference m_ActiveConference;

		private bool m_BluetoothStreamingStatus;

		#region Properties

		private bool BluetoothStreamingStatus
		{
			get { return m_BluetoothStreamingStatus; }
			set
			{
				if (m_BluetoothStreamingStatus == value)
					return;

				m_BluetoothStreamingStatus = value;

				if (m_BluetoothStreamingStatus)
					StartConference();
				else
					EndConference();
			}
		}

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio; } }

		#endregion

		#region Constructors

		public TesiraExUbtBluetoothConferenceControl([NotNull] TesiraExUbtBluetoothDevice parent, int id) 
			: base(parent, id)
		{
			SupportedConferenceFeatures = eConferenceFeatures.None;
		}

		#endregion

		#region Methods

		public override IEnumerable<TraditionalConference> GetConferences()
		{
			if (m_ActiveConference != null)
				yield return m_ActiveConference;
		}

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			return eDialContextSupport.Unsupported;
		}

		public override void Dial(IDialContext dialContext)
		{
			throw new NotSupportedException();
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			throw new NotSupportedException();
		}

		public override void SetAutoAnswer(bool enabled)
		{
			throw new NotSupportedException();
		}

		public override void SetPrivacyMute(bool enabled)
		{
			throw new NotSupportedException();
		}

		public override void SetCameraMute(bool mute)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

		private void StartConference()
		{
			EndConference();

			DateTime now = IcdEnvironment.GetUtcTime();

			ThinTraditionalParticipant participant = new ThinTraditionalParticipant
			{
				HangupCallback = HangupParticipant
			};
			participant.SetName(m_Block.ConnectedDeviceName);
			participant.SetAnswerState(eCallAnswerState.Answered);
			participant.SetCallType(eCallType.Audio);
			participant.SetDialTime(now);
			participant.SetStart(now);
			participant.SetStatus(eParticipantStatus.Connected);

			m_ActiveConference = new TraditionalConference();
			m_ActiveConference.AddParticipant(participant);

			OnConferenceAdded.Raise(this, new ConferenceEventArgs(m_ActiveConference));
		}

		private void EndConference()
		{
			if (m_ActiveConference == null)
				return;

			m_ActiveConference.Hangup();

			TraditionalConference endedConference = m_ActiveConference;
			m_ActiveConference = null;

			OnConferenceRemoved.Raise(this, new ConferenceEventArgs(endedConference));
		}

		private void HangupParticipant(ThinTraditionalParticipant participant)
		{
			participant.SetStatus(eParticipantStatus.Disconnected);
			participant.SetEnd(IcdEnvironment.GetUtcTime());

			if (m_ActiveConference != null)
				m_ActiveConference.RemoveParticipant(participant);
		}

		#endregion

		#region Block Callbacks

		public void SetBlock(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == m_Block)
				return;

			Unsubscribe(m_Block);
			m_Block = block;
			Subscribe(m_Block);

			UpdateStatus();
		}

		private void UpdateStatus()
		{
			BluetoothStreamingStatus = m_Block != null && m_Block.StreamingStatus;
		}

		private void Subscribe(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == null)
				return;

			block.OnStreamingStatusChanged += BlockOnStreamingStatusChanged;
		}

		private void Unsubscribe(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == null)
				return;

			block.OnStreamingStatusChanged -= BlockOnStreamingStatusChanged;
		}

		private void BlockOnStreamingStatusChanged(object sender, BoolEventArgs args)
		{
			BluetoothStreamingStatus = args.Data;
		}

		#endregion
	}
}