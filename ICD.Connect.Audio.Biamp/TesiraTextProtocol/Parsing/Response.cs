using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	public sealed class Response
	{
		public const string SUCCESS = "+OK";
		public const string ERROR = "-ERR";
		public const string FEEDBACK = "!";
		public const string CANNOT_DELIVER = "-CANNOT_DELIVER";
		public const string GENERAL_FAILURE = "-GENERAL_FAILURE";

		public const string PUBLISH_TOKEN = "publishToken";

		public enum eResponseType
		{
			Success,
			Error,
			Feedback,
			CannotDeliver,
			GeneralFailure
		}

		private static readonly Dictionary<string, eResponseType> s_ResponseTypeTable =
			new Dictionary<string, eResponseType>
			{
				{SUCCESS, eResponseType.Success},
				{ERROR, eResponseType.Error},
				{FEEDBACK, eResponseType.Feedback},
				{CANNOT_DELIVER, eResponseType.CannotDeliver},
				{GENERAL_FAILURE, eResponseType.GeneralFailure}
			};

		private readonly eResponseType m_ResponseType;
		private readonly string m_Message;
		private readonly ControlValue m_Values;

		#region Properties

		/// <summary>
		/// Gets the success state of the response.
		/// </summary>
		public eResponseType ResponseType { get { return m_ResponseType; } }

		/// <summary>
		/// Gets the message body of the response.
		/// </summary>
		public string Message { get { return m_Message; } }

		/// <summary>
		/// Gets the key/value pairs contained in the response. 
		/// </summary>
		public ControlValue Values { get { return m_Values; } }

		/// <summary>
		/// Shorthand for getting the subscription feedback publishToken value.
		/// </summary>
		public string PublishToken
		{
			get
			{
				Value value = Values[PUBLISH_TOKEN] as Value;
				return value == null ? null : value.StringValue;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="responseType"></param>
		/// <param name="message"></param>
		/// <param name="values"></param>
		private Response(eResponseType responseType, string message, ControlValue values)
		{
			m_ResponseType = responseType;
			m_Message = message;
			m_Values = values;
		}

		/// <summary>
		/// Instantiates a Repsonse from serial rx data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static Response Deserialize(string data)
		{
			string key = s_ResponseTypeTable.Keys.Where(data.StartsWith).First();
			eResponseType responseType = s_ResponseTypeTable[key];

			string remaining = data.Substring(key.Length);

			// Look for the first quote or open bracket
			int index = remaining.IndexOfAny(new []{'"', '{'});
			if (index == -1)
				return new Response(responseType, remaining.Trim(), new ControlValue());

			// The message is everything from the key to the values.
			string message = remaining.Substring(0, index).Trim();

			// The values are the remainder of the string.
			string serializedValues = remaining.Substring(index).Trim();

			// Bit of a hack. Often the response contains a collection of values,
			// but doesn't enclose them in control brackets.
			if (!serializedValues.StartsWith('{'))
				serializedValues = '{' + serializedValues + '}';

			ControlValue values = ControlValue.Deserialize(serializedValues);

			return new Response(responseType, message, values);
		}

		#endregion
	}
}
