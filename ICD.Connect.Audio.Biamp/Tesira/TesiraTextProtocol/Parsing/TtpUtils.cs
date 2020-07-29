using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing
{
	public static class TtpUtils
	{
		public const char CR = '\x0D';
		public const char LF = '\x0A';

		/// <summary>
		/// Takes serialized data in the form "A":1 and returns the value 1, outputs the key A.
		/// </summary>
		/// <param name="serialized"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string RemoveKey(string serialized, out string key)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

			serialized = serialized.Trim();
			key = null;

			if (!serialized.StartsWith('"'))
				return serialized;

			int delimiterIndex = serialized.IndexOf(':');
			if (delimiterIndex < 0)
				return serialized;

			key = RemoveQuotes(serialized.Substring(0, delimiterIndex));

			return serialized.Substring(delimiterIndex + 1).Trim();
		}

		/// <summary>
		/// Removes the " quotes from the start and end of a key or serialized string value.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static string RemoveQuotes(string serialized)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

			serialized = serialized.Trim();

			int startIndex = serialized.StartsWith('"') ? 1 : 0;
			int endIndex = serialized.EndsWith('"') ? serialized.Length - 1 : serialized.Length;

			return serialized.Substring(startIndex, endIndex - startIndex);
		}

		/// <summary>
		/// Deserializes the given value serial to the correct type.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static IValue DeserializeValue(string serialized)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

			string unused;
			serialized = RemoveKey(serialized, out unused).Trim();

			switch (serialized[0])
			{
				case '{':
					return ControlValue.Deserialize(serialized);

				case '[':
					return ArrayValue.Deserialize(serialized);
				
				default:
					return Value.Deserialize(serialized);
			}
		}

		/// <summary>
		/// Takes serialized data in the form {"a":1 "b":"2" "c":[3]} and returns the deserialized
		/// values with their keys.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<string, IValue>> GetKeyedValues(string serialized)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

			serialized = serialized.Trim();

			if (!serialized.StartsWith('{'))
				throw new FormatException("Expected { bracket");
			if (!serialized.EndsWith('}'))
				throw new FormatException("Expected } bracket");

			serialized = serialized.Substring(1, serialized.Length - 2);

			foreach (string item in SplitValues(serialized))
			{
				string key;
				RemoveKey(item, out key);

				if (key == null)
				{
					string message = string.Format("\"{0}\" has no key", item);
					throw new FormatException(message);
				}

				yield return new KeyValuePair<string, IValue>(key, DeserializeValue(item));
			}
		}

		/// <summary>
		/// Takes serialized data in the form [ 1 "2" [3] ] and returns the deserialized values.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static IEnumerable<IValue> GetArrayValues(string serialized)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

			serialized = serialized.Trim();

			if (!serialized.StartsWith('['))
				throw new FormatException("Expected [ bracket");
			if (!serialized.EndsWith(']'))
				throw new FormatException("Expected ] bracket");

			serialized = serialized.Substring(1, serialized.Length - 2);

			return SplitArrayValues(serialized)
				.Select(s => DeserializeValue(s));
		}

		/// <summary>
		/// Given a sequence of values or key/value pairs, returns the individual serialized items.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static IEnumerable<string> SplitValues(string serialized)
		{
			if (serialized == null)
				throw new ArgumentNullException("serialized");

            // The split logic doesn't handle values with spaces very well, so we
            // simply append values without keys to the previous item
            string previous = null;

            foreach (string item in SplitArrayValues(serialized))
            {
                if (item.Contains(':'))
                {
                    // Clear out the previous item
                    if (previous != null)
                    {
                        yield return previous;
                        previous = null;
                    }
                }

                if (previous == null)
                    previous = item;
                else
                    previous += " " + item;
            }

            if (previous != null)
                yield return previous;
        }

        /// <summary>
        /// Given a sequence of values, returns the individual serialized items.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        private static IEnumerable<string> SplitArrayValues(string serialized)
        {
            if (serialized == null)
                throw new ArgumentNullException("serialized");

            int controlCount = 0;
            int arrayCount = 0;
            bool quoted = false;
            bool escaped = false;
            bool skipWhitespace = true;
            string output;

            StringBuilder builder = new StringBuilder();

            foreach (char character in serialized)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (skipWhitespace)
                        continue;

                    if (arrayCount == 0 &&
                        controlCount == 0 &&
                        !quoted &&
                        !escaped)
                    {
                        output = builder.Pop().Trim();
                        if (!string.IsNullOrEmpty(output))
                            yield return output;
                    }
                }

                switch (character)
                {
                    case '{':
                        if (!escaped)
                            controlCount++;
                        break;
                    case '}':
                        if (!escaped)
                            controlCount--;
                        break;

                    case '[':
                        if (!escaped)
                            arrayCount++;
                        break;
                    case ']':
                        if (!escaped)
                            arrayCount--;
                        break;

                    case '"':
                        if (!escaped)
                            quoted = !quoted;
                        break;
                }

                // Handle escaped characters
                escaped = !escaped && character == '\\';

                // Skip whitespace if we are at a <key>: and looking for the value.
                skipWhitespace = character == ':' &&
                                 arrayCount == 0 &&
                                 controlCount == 0 &&
                                 !quoted &&
                                 !escaped;

                builder.Append(character);
            }

            output = builder.Pop().Trim();

            if (!string.IsNullOrEmpty(output))
                yield return output;
        }
	}
}
