using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"id": 1234,
	///		"method": "Control.Get",
	///		"params": ["MainGain"]
	/// }
	/// </summary>
	public sealed class ControlGetRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "Control.Get";

		private readonly List<string> m_Controls;

		public override string Method { get { return METHOD_VALUE; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ControlGetRpc()
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

			writer.WriteStartArray();
			{
				foreach (string control in m_Controls)
					writer.WriteValue(control);
			}
			writer.WriteEndArray();
		}
	}
}
