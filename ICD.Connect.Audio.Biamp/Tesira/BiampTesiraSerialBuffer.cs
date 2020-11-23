using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Audio.Biamp.Tesira
{
	public sealed class BiampTesiraSerialBuffer : AbstractSerialBuffer
	{
		private const string WELCOME_TEXT = "Welcome to the Tesira Text Protocol Server...";
		private const string PUBLISH_RESPONSE = "! \"publishToken\":";

		private static readonly char[] s_Delimiters =
		{
			TtpUtils.CR,
			TtpUtils.LF
		};

		/// <summary>
		/// Raised when a telnet header is discovered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSerialTelnetHeader;

		/// <summary>
		/// Raised when a subscription response message is discovered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSubscribeResponse;

		/// <summary>
		/// Raised when the welcome message is discovered.
		/// </summary>
		public event EventHandler OnWelcomeMessageReceived;

		private string m_Remainder;

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_Remainder = null;
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		protected override IEnumerable<string> Process(string data)
		{
			// Prepend anything left from the previous pass
			m_Remainder = (m_Remainder ?? string.Empty) + data;
			if (m_Remainder.Length == 0)
				yield break;

			// First check for telnet negotiation
			while (m_Remainder.Length >= 3 && m_Remainder[0] == TelnetCommand.HEADER)
			{
				string output = m_Remainder.Substring(0, 3);
				m_Remainder = m_Remainder.Substring(3);
				OnSerialTelnetHeader.Raise(this, new StringEventArgs(output));
			}

			// Look for delimiters
			while (m_Remainder.Length > 0)
			{
				int index = m_Remainder.IndexOfAny(s_Delimiters);
				if (index < 0)
					break;

				string output = m_Remainder.Substring(0, index);
				m_Remainder = m_Remainder.Substring(index + 1);

				if (string.IsNullOrEmpty(output))
					continue;

				if (output == WELCOME_TEXT)
				{
					OnWelcomeMessageReceived.Raise(this);
					continue;
				}

				if (output.StartsWith(PUBLISH_RESPONSE))
				{
					OnSubscribeResponse.Raise(this, new StringEventArgs(output));
					continue;
				}

				yield return output;
			}
		}

		public static bool EchoComparer(ISerialData command, string response)
		{
			if (command == null || response == null)
				return false;
			
			return response.Equals(command.Serialize().Trim(s_Delimiters));
		}
	}
}
