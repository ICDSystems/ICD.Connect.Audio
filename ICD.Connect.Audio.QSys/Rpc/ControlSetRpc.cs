using System;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"id": 1234,
	///		"method": "Control.Set",
	///		"params": {
	///			"Name": "MainGain",
	///			"Value": -12
	///		}
	/// }
	/// </summary>
	public sealed class ControlSetRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";
		private const string VALUE_PROPERTY = "Value";

		private const string METHOD_VALUE = "Control.Set";

		public override string Method { get { return METHOD_VALUE; } }

		public string Control { get; set; }

		public object Value { get; set; }

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
				// Control name
				writer.WritePropertyName(NAME_PROPERTY);
				writer.WriteValue(Control);

				// Control value
				writer.WritePropertyName(VALUE_PROPERTY);
				writer.WriteValue(Value);
			}
			writer.WriteEndObject();
		}
	}
}
