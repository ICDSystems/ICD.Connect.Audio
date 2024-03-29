﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing
{
	/// <summary>
	/// Represents a singular TTP value. Serialized data surrounded with quotes is assumed to be a string.
	/// </summary>
	public sealed class Value : AbstractValue<Value>
	{
		// hh:mm:ss:MM:DD:YYYY
		// hh = Hours
		// mm = minutes
		// ss = Seconds. Leap seconds (SS=60) specification are forbidden.
		// MM =month of year 1-12
		// DD =day of month 1-(28,29,30,31) according to the month and year
		// YYYY = Year must be >= 2000
		// Spaces are not permitted after the : and before YYYY so “: 2000” is not valid.
		private const string DATETIME_FORMAT = "HH:mm:ss:MM:dd:yyyy";

		/// <summary>
		/// In some cases (such as caller id) the value will be a string of quote delimited strings.
		/// E.g. the string literal
		///		"cid":"\"01131947\"\"test\"\"\"" 
		/// Becomes the sequence of strings
		///		"01131947", "test", ""
		/// </summary>
		private const string GET_STRING_VALUES_REGEX = @"\\\""(?'data'.*?)\\\""";

		private string m_Value;

		#region Properties

		public bool IsString { get { return m_Value.StartsWith('"') && m_Value.EndsWith('"'); } }

		public bool IsNull { get { return string.IsNullOrEmpty(m_Value); } }

		/// <summary>
		/// Returns the wrapped value as an integer.
		/// </summary>
		public int IntValue {
			get
			{
				int output;
				if (StringUtils.TryParse(m_Value, out output))
					return output;

				string message = string.Format("Wrapped serial {0} does not represent an int value",
											   StringUtils.ToRepresentation(m_Value));
				throw new FormatException(message);
			}
		}

		/// <summary>
		/// Returns the wrapped value as a float.
		/// </summary>
		public float FloatValue
		{
			get
			{
				float output;
				if (StringUtils.TryParse(m_Value, out output))
					return output;

				string message = string.Format("Wrapped serial {0} does not represent a float value",
											   StringUtils.ToRepresentation(m_Value));
				throw new FormatException(message);
			}
		}

		/// <summary>
		/// Returns the wrapped value as a string.
		/// </summary>
		public string StringValue
		{
			get
			{
				if (IsString)
					return m_Value.Substring(1, m_Value.Length - 2);

				string message = string.Format("Wrapped serial {0} does not represent a string value",
				                               StringUtils.ToRepresentation(m_Value));
				throw new FormatException(message);
			}
		}

		/// <summary>
		/// Returns the wrapped value as a bool.
		/// </summary>
		public bool BoolValue
		{
			get
			{
				bool output;
				if (StringUtils.TryParse(m_Value, out output))
					return output;

				string message = string.Format("Wrapped serial {0} does not represent a bool value",
											   StringUtils.ToRepresentation(m_Value));
				throw new FormatException(message);
			}
		}

		/// <summary>
		/// Returns the wrapped value as a DateTime.
		/// </summary>
		public DateTime DateTimeValue
		{
			get
			{
				try
				{
					return DateTime.ParseExact(m_Value, DATETIME_FORMAT, CultureInfo.InvariantCulture);
				}
				catch (FormatException e)
				{
					string message = string.Format("Wrapped serial {0} does not represent a DateTime value",
					                               StringUtils.ToRepresentation(m_Value));
					throw new FormatException(message, e);
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public Value()
			: this(null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="value"></param>
		public Value(object value)
		{
			if (value == null)
				m_Value = string.Empty;
			else if (value is string)
				m_Value = string.Format("\"{0}\"", value);
			else if (value is DateTime)
				m_Value = ((DateTime)value).ToString(DATETIME_FORMAT);
			else
				m_Value = value.ToString();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Creates a value from an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="ttpEnumMap">Maps the TTP string representation to a C# object.</param>
		/// <returns></returns>
		public static Value FromObject<T>(T value, IDictionary<string, T> ttpEnumMap)
		{
			if (ttpEnumMap == null)
				throw new ArgumentNullException("ttpEnumMap");

			return FromObject(value, ttpEnumMap.GetKey);
		}

		/// <summary>
		/// Creates a value from an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="serialize">Serializes the object to TTP.</param>
		/// <returns></returns>
		public static Value FromObject<T>(T value, Func<T, string> serialize)
		{
			if (serialize == null)
				throw new ArgumentNullException("serialize");

			string key = serialize(value);
			return Deserialize(key);
		}

		/// <summary>
		/// Returns the value as a string representation.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			return m_Value;
		}

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		protected override bool CompareEquality(Value other)
		{
			return other != null && m_Value == other.m_Value;
		}

		/// <summary>
		/// Deserializes the given serial to a Value.
		/// </summary>
		/// <param name="serial"></param>
		/// <returns></returns>
		public static Value Deserialize(string serial)
		{
			return new Value {m_Value = serial};
		}

		/// <summary>
		/// Gets the value as an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectMap">Maps the TTP string representation to a C# object.</param>
		/// <returns></returns>
		[Obsolete("Pass an additional default value")]
		public T GetObjectValue<T>(IDictionary<string, T> objectMap)
		{
			if (objectMap == null)
				throw new ArgumentNullException("objectMap");

			return GetObjectValue(arg =>
			                      {
				                      if (!objectMap.ContainsKey(m_Value))
				                      {
					                      string message = string.Format("Could not find key \"{0}\"", arg);
					                      throw new KeyNotFoundException(message);
				                      }
				                      return objectMap[m_Value];
			                      });
		}

		/// <summary>
		/// Gets the value as an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectMap">Maps the TTP string representation to a C# object.</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T GetObjectValue<T>(IDictionary<string, T> objectMap, T defaultValue)
		{
			if (objectMap == null)
				throw new ArgumentNullException("objectMap");

			return GetObjectValue(arg => objectMap.ContainsKey(m_Value) ? objectMap[m_Value] : defaultValue);
		}

		/// <summary>
		/// Gets the value as an object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="deserialize">Deserializes the TTP object.</param>
		/// <returns></returns>
		public T GetObjectValue<T>(Func<string, T> deserialize)
		{
			if (deserialize == null)
				throw new ArgumentNullException("deserialize");

			return deserialize(m_Value);
		}

		/// <summary>
		/// In some cases (such as caller id) the value will be a string of quote delimited strings.
		/// E.g. the string literal
		///		"cid":"\"01131947\"\"test\"\"\"" 
		/// Becomes the sequence of strings
		///		"01131947", "test", ""
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetStringValues()
		{
			return Regex.Matches(StringValue, GET_STRING_VALUES_REGEX)
			            .Cast<Match>()
			            .Select(match => match.Groups["data"].Value);
		}

		#endregion
	}
}
