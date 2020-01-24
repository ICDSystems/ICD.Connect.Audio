using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Audio.Biamp
{
	public sealed class BiampTesiraSerialBuffer : ISerialBuffer
	{
		public const string WELCOME_TEXT = "Welcome to the Tesira Text Protocol Server...";

		public event EventHandler<StringEventArgs> OnCompletedSerial;
		public event EventHandler<StringEventArgs> OnSerialTelnetHeader;
		public event EventHandler OnWelcomeMessageReceived;

		private string m_Remainder;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private readonly char[] m_Delimiters =
		{
			TtpUtils.CR,
			TtpUtils.LF
		};

		/// <summary>
		/// Constructor.
		/// </summary>
		public BiampTesiraSerialBuffer()
		{
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(string data)
		{
			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_ParseSection.Enter();
			m_QueueSection.Enter();

			try
			{
				m_Remainder = null;
				m_Queue.Clear();
			}
			finally
			{
				m_ParseSection.Leave();
				m_QueueSection.Leave();
			}
		}

		/// <summary>
		/// Searches the enqueued serial data for the delimiter character.
		/// Complete strings are raised via the OnCompletedString event.
		/// </summary>
		private void Parse()
		{
			if (!m_ParseSection.TryEnter())
				return;

			try
			{
				string data = null;
				
				while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
				{
					// Prepend anything left from the previous pass
					m_Remainder = (m_Remainder ?? string.Empty) + data;
					if (m_Remainder.Length == 0)
						continue;

					// First check for telnet negotiation
					while (m_Remainder.Length >= 3 && m_Remainder[0] == TelnetControl.HEADER)
					{
						string output = m_Remainder.Substring(0, 3);
						m_Remainder = m_Remainder.Substring(3);
						OnSerialTelnetHeader.Raise(this, new StringEventArgs(output));
					}

					// Look for delimiters
					while (m_Remainder.Length > 0)
					{
						int index = m_Remainder.IndexOfAny(m_Delimiters);
						if (index < 0)
							break;

						string output = m_Remainder.Substring(0, index);
						m_Remainder = m_Remainder.Substring(index + 1);

						if (output == WELCOME_TEXT)
						{
							OnWelcomeMessageReceived.Raise(this);
							continue;
						}

						if (!string.IsNullOrEmpty(output))
						{
							OnCompletedSerial.Raise(this, new StringEventArgs(output));
						}
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}
	}
}
