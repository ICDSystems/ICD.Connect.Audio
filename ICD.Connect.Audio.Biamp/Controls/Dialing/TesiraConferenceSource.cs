﻿using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
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
		private DateTime? m_Start;
		private DateTime? m_End;

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

				Log(eSeverity.Informational, "Name set to {1}", this, m_Name);

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

				Log(eSeverity.Informational, "Number set to {1}", this, m_Number);

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

				Log(eSeverity.Informational, "Status set to {1}", this, m_Status);

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

				Log(eSeverity.Informational, "AnswerState set to {1}", this, m_AnswerState);

				OnAnswerStateChanged.Raise(this, new ConferenceSourceAnswerStateEventArgs(m_AnswerState));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? Start
		{
			get { return m_Start; }
			internal set
			{
				if (value == m_Start)
					return;

				m_Start = value;

				Log(eSeverity.Informational, "Start set to {0}", m_Start);
			}
		}

		/// <summary>
		/// The time the call ended.
		/// </summary>
		public DateTime? End
		{
			get { return m_End; }
			internal set
			{
				if (value == m_End)
					return;

				m_End = value;

				Log(eSeverity.Informational, "Start set to {0}",  m_Start);
			}
		}

		/// <summary>
		/// Gets the source type.
		/// </summary>
		eConferenceSourceType IConferenceSource.SourceType { get { return eConferenceSourceType.Audio; } }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		ICamera IConferenceSource.Camera { get { return null; } }

		[PublicAPI]
		internal Action AnswerCallback { get; set; }

		[PublicAPI]
		internal Action HoldCallback { get; set; }

		[PublicAPI]
		internal Action ResumeCallback { get; set; }

		[PublicAPI]
		internal Action HangupCallback { get; set; }

		[PublicAPI]
		internal Action<string> SendDtmfCallback { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the string representation for this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}(Name={1}, Number={2})", GetType().Name, StringUtils.ToRepresentation(Name),
			                     StringUtils.ToRepresentation(Number));
		}

		/// <summary>
		/// Answers the incoming source.
		/// </summary>
		public void Answer()
		{
			if (AnswerCallback == null)
				throw new InvalidOperationException("No AnswerCallback assigned");

			AnswerCallback();
		}

		/// <summary>
		/// Holds the source.
		/// </summary>
		public void Hold()
		{
			if (HoldCallback == null)
				throw new InvalidOperationException("No HoldCallback assigned");

			HoldCallback();
		}

		/// <summary>
		/// Resumes the source.
		/// </summary>
		public void Resume()
		{
			if (ResumeCallback == null)
				throw new InvalidOperationException("No ResumeCallback assigned");

			ResumeCallback();
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Hangup()
		{
			if (HangupCallback == null)
				throw new InvalidOperationException("No HangupCallback assigned");

			HangupCallback();
		}

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		public void SendDtmf(string data)
		{
			if (SendDtmfCallback == null)
				throw new InvalidOperationException("No SendDtmfCallback assigned");

			SendDtmfCallback(data);
		}

		#endregion

		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} - {1}", this, message);
			ServiceProvider.GetService<ILoggerService>().AddEntry(severity, message, args);
		}
	}
}
