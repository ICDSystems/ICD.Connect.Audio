using System;
using System.Text.RegularExpressions;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Shure
{
	public sealed class ShureMxaSerialData : ISerialData
	{
		private const string REGEX = @"< (?'type'GET|SET|REP) ((?'channel'[\d]) )?(?'command'[\S]+) (?'value'[\S]+) >";
		private const string SAMPLE_REGEX = @"< (?'type'SAMPLE) (?'value'([\d]{3} )+)>";

		public const string GET = "GET";
		public const string SET = "SET";
		public const string REP = "REP";
		public const string SAMPLE = "SAMPLE";

		public string Type { get; set; }

		public int? Channel { get; set; }

		public string Command { get; set; }

		public string Value { get; set; }

		public string[] SampleValues { get; set; }

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return Channel.HasValue
				? string.Format("< {0} {1} {2} {3} >", Type, Channel.Value, Command, Value)
				: string.Format("< {0} {1} {2} >", Type, Command, Value);
		}

		/// <summary>
		/// Deserialize the string to a data instance.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static ShureMxaSerialData Deserialize(string data)
		{
			Match match = Regex.Match(data, REGEX);
			if (!match.Success)
				return DeserializeSample(data);

			string channel = match.Groups["channel"].Value;

			return new ShureMxaSerialData
			{
				Type = match.Groups["type"].Value,
				Channel = string.IsNullOrEmpty(channel) ? (int?)null : int.Parse(channel),
				Command = match.Groups["command"].Value,
				Value = match.Groups["value"].Value,
			};
		}

		/// <summary>
		/// Deserialize the string to a data instance.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static ShureMxaSerialData DeserializeSample(string data)
		{
			Match match = Regex.Match(data, SAMPLE_REGEX);
			if (!match.Success)
				throw new FormatException();

				return new ShureMxaSerialData
				{
					Type = match.Groups["type"].Value,
					SampleValues = match.Groups["value"].Value.Trim().Split()
				};
			}
	}
}
