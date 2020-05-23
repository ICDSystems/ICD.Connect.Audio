using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Audio.Shure
{
	public sealed class ShureMicSerialBuffer : AbstractSerialBuffer
	{
		private static readonly char[] s_Delimiters =
		{
			'<',
			'>'
		};

		private readonly StringBuilder m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShureMicSerialBuffer()
		{
			m_RxData = new StringBuilder();
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData.Clear();
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
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
							yield return m_RxData.Pop();
						}
						break;
				}

				data = data.Substring(index + 1);
			}
		}
	}
}
