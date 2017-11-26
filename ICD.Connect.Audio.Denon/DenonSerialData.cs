using System;
using System.Text.RegularExpressions;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Denon
{
	public sealed class DenonSerialData : ISerialData
	{
		public const char DELIMITER = '\r';
		private const char REQUEST = '?';

		private const string REGEX = @"([a-zA-Z]+)\ *([\d]*)";

		private readonly string m_Data;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DenonSerialData(string data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			if (data[data.Length - 1] != DELIMITER)
				data = data + DELIMITER;

			m_Data = data;
		}

		/// <summary>
		/// Creates a serial data instance for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static DenonSerialData Command(string command)
		{
			return new DenonSerialData(command);
		}

		/// <summary>
		/// Creates a serial data instance for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static DenonSerialData Command(string command, object parameter)
		{
			command = string.Format(command, parameter);
			return Command(command);
		}

		/// <summary>
		/// Creates a serial data instance for the given command request.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static DenonSerialData Request(string command)
		{
			return new DenonSerialData(command + REQUEST);
		}

		/// <summary>
		/// Gets the command string from the data.
		/// </summary>
		/// <returns></returns>
		public string GetCommand()
		{
			Regex regex = new Regex(REGEX);
			Match match = regex.Match(m_Data);

			return match.Success ? match.Groups[1].Value : null;
		}

		/// <summary>
		/// Gets the numeric portion from the data.
		/// </summary>
		/// <returns></returns>
		public string GetValue()
		{
			Regex regex = new Regex(REGEX);
			Match match = regex.Match(m_Data);

			return match.Success ? match.Groups[2].Value : null;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return m_Data;
		}
	}
}
