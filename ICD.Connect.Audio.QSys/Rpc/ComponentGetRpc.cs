using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
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

		public override string Id { get { return RpcUtils.RPCID_NAMED_COMPONENT_GET; }}

		public string Name { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ComponentGetRpc(string componentName, string control)
		{
			Name = componentName;
			m_Controls = new List<string>{control};
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ComponentGetRpc(string componentName, IEnumerable<string> controls)
		{
			Name = componentName;
			m_Controls = controls.ToList();
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
			writer.WritePropertyName(CONTROLS_PROPERTY);
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
