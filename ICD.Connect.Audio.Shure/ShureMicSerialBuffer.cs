using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Audio.Shure
{
	public sealed class ShureMicSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private static readonly char[] s_Delimiters =
		{
			'<',
			'>'
		};

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShureMicSerialBuffer()
		{
			m_RxData = new StringBuilder();
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

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

		#endregion

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
					while (true)
					{
						int index = data.IndexOfAny(s_Delimiters);

						// Simple case - No delimiters in the data
						if (index < 0)
						{
							if (m_RxData.Length > 0)
								m_RxData.Append(data);
							break;
						}

						char token = data[index];

						switch (token)
						{
							case '<':
								// Trim any leading nonsense
								m_RxData.Clear();
								m_RxData.Append('<');
								break;

							case '>':
								if (m_RxData.Length > 0)
								{
									m_RxData.Append(data.Substring(0, index + 1));
									string output = m_RxData.Pop();
									OnCompletedSerial.Raise(this, new StringEventArgs(output));
								}
								break;
						}	

						data = data.Substring(index + 1);
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