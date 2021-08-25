#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc":"2.0",
	///		"method":"NoOp",
	///		"params":{
	///		}
	/// }
	/// </summary>
	internal sealed class NoOpRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "NoOp";

		public override string Method { get { return METHOD_VALUE; } }

		public override string Id { get { return RpcUtils.RPCID_NO_OP; } }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

		}
	}
}
