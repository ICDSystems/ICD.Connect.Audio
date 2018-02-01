using System;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"method": "StatusGet",
	///		"id": 1234,
	///		"params": 0
	/// }
	/// </summary>
	public sealed class StatusGetRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "StatusGet";

		public override string Method { get { return METHOD_VALUE; } }

		public override string Id { get { return RpcUtils.RPCID_STATUS_GET; } }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteValue(0);
		}

		protected override void WriteParamsContainer(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			SerializeParams(writer);
		}
	}
}
