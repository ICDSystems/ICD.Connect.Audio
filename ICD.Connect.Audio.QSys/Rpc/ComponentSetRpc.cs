using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"id": 1234,
	///		"method": "Component.Set",
	///		"params": {
	///			"Name": "My APM",
	///			"Controls": [
	///				{
	///					"Name": "ent.xfade.gain",
	///					"Value": -100.0,
	///					"Ramp": 2.0
	///				}
	///			]
	///		}
	/// }
	/// </summary>
	public sealed class ComponentSetRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";
		private const string CONTROLS_PROPERTY = "Controls";

		private const string METHOD_VALUE = "Component.Set";

		private readonly List<ControlValueInfo> m_ControlValues; 

		public override string Method { get { return METHOD_VALUE; } }

		public string Name { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ComponentSetRpc()
		{
			m_ControlValues = new List<ControlValueInfo>();
		}

		public void AddControlValue(string name, object value)
		{
			AddControlValue(name, value, null);
		}

		public void AddControlValue(string name, object value, float? ramp)
		{
			ControlValueInfo controlValue = new ControlValueInfo(name, value, ramp);
			m_ControlValues.Add(controlValue);
		}

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartObject();
			{
				// Name
				writer.WritePropertyName(NAME_PROPERTY);
				writer.WriteValue(Name);

				// Controls
				writer.WritePropertyName(CONTROLS_PROPERTY);
				writer.WriteStartArray();
				{
					foreach (ControlValueInfo info in m_ControlValues)
						info.Serialize(writer);
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}

		private struct ControlValueInfo
		{
			private const string CONTROL_NAME_PROPERTY = "Name";
			private const string CONTROL_VALUE_PROPERTY = "Value";
			private const string CONTROL_RAMP_PROPERTY = "Ramp";

			private readonly string m_Name;
			private readonly object m_Value;
			private readonly float? m_Ramp;

			public string Name { get { return m_Name; } }

			public object Value { get { return m_Value; } }

			public float? Ramp { get { return m_Ramp; } }

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public ControlValueInfo(string name, object value)
				: this(name, value, null)
			{
			}

			/// <summary>
			/// Contructor.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			/// <param name="ramp"></param>
			public ControlValueInfo(string name, object value, float? ramp)
			{
				m_Name = name;
				m_Value = value;
				m_Ramp = ramp;
			}

			/// <summary>
			/// Writes the control value info to JSON.
			/// </summary>
			/// <param name="writer"></param>
			public void Serialize(JsonWriter writer)
			{
				if (writer == null)
					throw new ArgumentNullException("writer");

				writer.WriteStartObject();
				{
					// Name
					writer.WritePropertyName(CONTROL_NAME_PROPERTY);
					writer.WriteValue(m_Name);

					// Value
					writer.WritePropertyName(CONTROL_VALUE_PROPERTY);
					writer.WriteValue(m_Value);

					// Ramp
					if (m_Ramp.HasValue)
					{
						writer.WritePropertyName(CONTROL_RAMP_PROPERTY);
						writer.WriteValue(m_Ramp.Value);
					}
				}
				writer.WriteEndObject();
			}
		}
	}
}
