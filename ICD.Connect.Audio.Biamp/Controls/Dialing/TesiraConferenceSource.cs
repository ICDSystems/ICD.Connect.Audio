using System;
using ICD.Common.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing
{
	/// <summary>
	/// Tesira dialing attribute interfaces have a fixed number of Call Appearances. This means
	/// that conference sources would be tied to a fixed index.
	/// 
	/// The TesiraConferenceSource is completely decoupled from the Call Appearances to avoid
	/// "overwriting" older, terminated sources, or allowing old sources to control new ones.
	/// </summary>
	public sealed class TesiraConferenceSource : IConferenceSource
	{
		public event EventHandler<ConferenceSourceAnswerStateEventArgs> OnAnswerStateChanged;
		public event EventHandler<ConferenceSourceStatusEventArgs> OnStatusChanged;
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler OnSourceTypeChanged;

		private string m_Name;
		private string m_Number;
		private eConferenceSourceStatus m_Status;
		private eConferenceSourceAnswerState m_AnswerState;

		#region Properties

		/// <summary>
		/// Gets the source name.
		/// </summary>
		public string Name
		{
			get { return m_Name; }
			internal set
			{
				if (value == m_Name)
					return;

				m_Name = value;

				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		/// <summary>
		/// Gets the source number.
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			internal set
			{
				if (value == m_Number)
					return;
				
				m_Number = value;

				OnNumberChanged.Raise(this, new StringEventArgs(m_Number));
			}
		}

		/// <summary>
		/// Call Status (Idle, Dialing, Ringing, etc)
		/// </summary>
		public eConferenceSourceStatus Status
		{
			get { return m_Status; }
			internal set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceSourceStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		public eConferenceSourceDirection Direction { get; internal set; }

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		public eConferenceSourceAnswerState AnswerState
		{
			get { return m_AnswerState; }
			internal set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				OnAnswerStateChanged.Raise(this, new ConferenceSourceAnswerStateEventArgs(m_AnswerState));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? Start { get; internal set; }

		/// <summary>
		/// The time the call ended.
		/// </summary>
		public DateTime? End { get; internal set; }

		/// <summary>
		/// Gets the source type.
		/// </summary>
		eConferenceSourceType IConferenceSource.SourceType { get { return eConferenceSourceType.Audio; } }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		ICamera IConferenceSource.Camera { get { return null; } }

		internal Action AnswerCallback { get; set; }
		internal Action HoldCallback { get; set; }
		internal Action ResumeCallback { get; set; }
		internal Action HangupCallback { get; set; }
		internal Action<string> SendDtmfCallback { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Answers the incoming source.
		/// </summary>
		public void Answer()
		{
			AnswerCallback();
		}

		/// <summary>
		/// Holds the source.
		/// </summary>
		public void Hold()
		{
			HoldCallback();
		}

		/// <summary>
		/// Resumes the source.
		/// </summary>
		public void Resume()
		{
			ResumeCallback();
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Hangup()
		{
			HangupCallback();
		}

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		public void SendDtmf(string data)
		{
			SendDtmfCallback(data);
		}

		#endregion
	}
}
