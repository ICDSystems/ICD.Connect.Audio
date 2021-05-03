﻿using System;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"method": "Component.GetComponents",
	///		"params": {},
	///		"id": 1234
	/// }
	/// </summary>
	public sealed class ComponentGetComponentsRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "Component.GetComponents";

		public override string Method { get { return METHOD_VALUE; } }

		public override string Id { get { return RpcUtils.RPCID_NAMED_COMPONENTS_GET_ALL; } }

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
