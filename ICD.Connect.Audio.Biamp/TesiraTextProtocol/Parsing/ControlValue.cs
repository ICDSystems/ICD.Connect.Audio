using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	/// <summary>
	/// A control is a value for AttributeCodes that looks like pseudo JSON.
	///	e.g. {"autoIPEnabled":false "ip":"192.168.1.210" "netmask":"255.255.255.0" "gateway":"0.0.0.0"}
	/// 
	/// Strings are wrapped with quotations.
	/// </summary>
	public sealed class ControlValue : AbstractValue<ControlValue>
	{
		private readonly Dictionary<string, IValue> m_Values;

		/// <summary>
		/// Gets the number of child values.
		/// </summary>
		public int Count { get { return m_Values.Count; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public ControlValue()
			: this(Enumerable.Empty<KeyValuePair<string, IValue>>())
		{
		}

        /// <summary>
        /// Constructor.
        /// </summary>
		public ControlValue(IDictionary<string, IValue> values)
			: this((IEnumerable<KeyValuePair<string, IValue>>)values)
		{
		}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="getKeyedValues"></param>
		public ControlValue(IEnumerable<KeyValuePair<string, IValue>> getKeyedValues)
		{
			m_Values = new Dictionary<string, IValue>();
			m_Values.AddRange(getKeyedValues);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the child value with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		[NotNull]
		public T GetValue<T>(string key)
			where T : IValue
		{
			IValue value;
			if (!m_Values.TryGetValue(key, out value))
			{
				string message1 = string.Format("{0} does not contain key \"{1}\"", GetType().Name, key);
				throw new KeyNotFoundException(message1);
			}

			if (value is T)
				return (T)value;

			string message2 = string.Format("Value \"{0}\" is not of type {1}", key, typeof(T).Name);
			throw new InvalidOperationException(message2);
		}

		/// <summary>
		/// Parses a string in the format
		/// {"autoIPEnabled":false "ip":"192.168.1.210" "netmask":"255.255.255.0" "gateway":"0.0.0.0"}
		/// </summary>
		/// <param name="serial"></param>
		/// <returns></returns>
		public static ControlValue Deserialize(string serial)
		{
			Dictionary<string, IValue> keyedValues = new Dictionary<string, IValue>();
			keyedValues.AddRange(TtpUtils.GetKeyedValues(serial));

			return new ControlValue(keyedValues);
		}

		/// <summary>
		/// Serializes the value to a string in TTP format.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			string contents = string.Join(" ", GetKvpsAsStrings().ToArray());

			StringBuilder builder = new StringBuilder();

			builder.Append('{');
			builder.Append(contents);
			builder.Append('}');

			return builder.ToString();
		}

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		protected override bool CompareEquality(ControlValue other)
		{
			return other != null && m_Values.DictionaryEqual(other.m_Values, (a, b) => a.CompareEquality(b));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns each pair of items in the values dict in the form "autoIPEnabled":false
		/// </summary>
		/// <returns></returns>
		private IEnumerable<string> GetKvpsAsStrings()
		{
			return m_Values.Select(kvp => GetKvpAsString(kvp.Key, kvp.Value));
		}

		/// <summary>
		/// Returns the key and value in the form "autoIPEnabled":false
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string GetKvpAsString(string key, IValue value)
		{
			string valueString = value == null ? string.Empty : value.Serialize();
			return string.Format("\"{0}\":{1}", key, valueString);
		}

		#endregion
	}
}
