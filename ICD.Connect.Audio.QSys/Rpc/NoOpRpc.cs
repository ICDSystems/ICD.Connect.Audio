using System;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc":"2.0",
	///		"method":"NoOp",
	///		"params":{
	///		}
	/// }
	/// </summary>
	public sealed class NoOpRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "NoOp";

		public override string Method { get { return METHOD_VALUE; } }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartObject();
			writer.WriteEndObject();
		}
	}
}
