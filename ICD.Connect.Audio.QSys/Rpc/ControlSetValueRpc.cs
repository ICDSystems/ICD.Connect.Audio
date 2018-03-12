using System;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
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
	public sealed class ControlSetValueRpc : AbstractControlSetRpc
	{
		private const string VALUE_PROPERTY = "Value";

		public object Value { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			base.SerializeParams(writer);

			// Control value
			// Write booleans as 1/0, since QSys doesn't support "True"
			writer.WritePropertyName(VALUE_PROPERTY);
			bool? valueBool = Value as bool?;
			if (valueBool != null)
				writer.WriteValue((bool)valueBool ? 1 : 0);
			else
				writer.WriteValue(Value);
		}

		public ControlSetValueRpc(INamedControl control, object value) : base(control)
	    {
	        Value = value;
	    }
	}
}
