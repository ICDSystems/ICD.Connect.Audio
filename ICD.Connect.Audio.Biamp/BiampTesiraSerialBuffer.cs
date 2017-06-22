using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Audio.Biamp
{
	public sealed class BiampTesiraSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
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
			m_RxData = new StringBuilder();
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
				m_RxData.Clear();
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
					foreach (char c in data)
					{
						m_RxData.Append(c);

						string stringData = m_RxData.ToString();

						// Negotiate telnet
						if (stringData.Length == 3 && stringData[0] == TelnetControl.HEADER)
						{
							string output = m_RxData.Pop();
							if (!string.IsNullOrEmpty(output))
								OnCompletedSerial.Raise(this, new StringEventArgs(output));
						}

						// Split on delimiters
						foreach (char delimiter in m_Delimiters)
						{
							if (!stringData.EndsWith(delimiter))
								continue;

							m_RxData.Remove(stringData.Length - 1, 1);
							if (m_RxData.Length == 0)
								continue;

							string output = m_RxData.Pop().Trim();
							if (!string.IsNullOrEmpty(output))
								OnCompletedSerial.Raise(this, new StringEventArgs(output));
							break;
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
