using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
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

		public override string Id { get { return RpcUtils.RPCID_NAMED_CONTROL_GET; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ControlGetRpc()
		{
			m_Controls = new List<string>();
		}

	    public ControlGetRpc(INamedControl control)
	    {
	        m_Controls = new List<string> {control.ControlName};
        }

		public ControlGetRpc(IEnumerable<INamedControl> controls)
		{
			m_Controls = controls.Select(c => c.ControlName).ToList();
		}

		/// <summary>
		/// Adds the control to the query.
		/// </summary>
		/// <param name="control"></param>
		public void AddControl(string control)
		{
			m_Controls.Add(control);
		}

		public void AddControl(INamedControl control)
		{
			m_Controls.Add(control.ControlName);
		}

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			foreach (string control in m_Controls)
				writer.WriteValue(control);

		}

		protected override void WriteParamsContainer(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartArray();
			{
				SerializeParams(writer);
			}
			writer.WriteEndArray();
		}
	}
}
