using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.ExUbt;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices.ExUbtBluetooth
{
	public sealed class TesiraExUbtBluetoothConferenceControl : AbstractConferenceDeviceControl<TesiraExUbtBluetoothDevice, ThinConference>
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

		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		private ExUbtBluetoothControlStatusBlock m_Block;

		[CanBeNull]
		private ThinConference m_ActiveConference;

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
			SupportedConferenceControlFeatures = eConferenceControlFeatures.None;
		}

		#endregion

		#region Methods

		public override IEnumerable<ThinConference> GetConferences()
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

		private void StartConference()
		{
			EndConference();

			DateTime now = IcdEnvironment.GetUtcTime();

			ThinConference conference = new ThinConference
			{
				Name = m_Block.ConnectedDeviceName,
				AnswerState = eCallAnswerState.Answered,
				CallType = eCallType.Audio,
				DialTime = now,
				StartTime = now,
				Status = eConferenceStatus.Connected
			};

			m_ActiveConference = conference;
			OnConferenceAdded.Raise(this, m_ActiveConference);
		}

		private void EndConference()
		{
			if (m_ActiveConference == null)
				return;

			m_ActiveConference.EndTime = IcdEnvironment.GetUtcTime();
			m_ActiveConference.Status = eConferenceStatus.Disconnected;

			ThinConference endedConference = m_ActiveConference;
			m_ActiveConference = null;

			OnConferenceRemoved.Raise(this, endedConference);
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