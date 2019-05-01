using System;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
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
	public abstract class AbstractControlSetRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";

		private const string METHOD_VALUE = "Control.Set";

		public override string Method { get { return METHOD_VALUE; } }

		public override string Id { get { return RpcUtils.RPCID_NAMED_CONTROL_SET; } }

		public string Control { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			// Control name
			writer.WritePropertyName(NAME_PROPERTY);
			writer.WriteValue(Control);

		}

		protected AbstractControlSetRpc(INamedControl control)
	    {
	        Control = control.ControlName;
	    }
	}
}
