using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"id": 1234,
	///		"method": "Component.Get",
	///		"params": {
	///			"Name": "My APM",
	///			"Controls": [
	///				{ "Name": "ent.xfade.gain" }
	///			]
	///		}
	/// }
	/// </summary>
	public sealed class ComponentGetRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";
		private const string CONTROLS_PROPERTY = "Controls";
		private const string CONTROLS_NAME_PROPERTY = "Name";

		private const string METHOD_VALUE = "Component.Get";

		private readonly List<string> m_Controls;

		public override string Method { get { return METHOD_VALUE; } }

		public string Name { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ComponentGetRpc()
		{
			m_Controls = new List<string>();
		}

		/// <summary>
		/// Adds the control to the query.
		/// </summary>
		/// <param name="control"></param>
		public void AddControl(string control)
		{
			m_Controls.Add(control);
		}

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			// Name
			writer.WritePropertyName(NAME_PROPERTY);
			writer.WriteValue(Name);

			// Controls
			writer.WriteStartArray();
			{
				foreach (string control in m_Controls)
				{
					writer.WriteStartObject();
					{
						writer.WritePropertyName(CONTROLS_NAME_PROPERTY);
						writer.WriteValue(control);
					}
					writer.WriteEndObject();
				}
			}
			writer.WriteEndArray();
		}
	}
}
