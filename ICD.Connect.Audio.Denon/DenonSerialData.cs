using System;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Denon
{
	public sealed class DenonSerialData : ISerialData
	{
		public const char DELIMITER = '\r';
		private const char REQUEST = '?';

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
			return m_Data.Replace(REQUEST.ToString(), "").Trim();
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
