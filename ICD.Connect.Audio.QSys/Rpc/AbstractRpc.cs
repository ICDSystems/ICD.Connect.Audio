using System;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public abstract class AbstractRpc : IRpc
	{
		private const string JSONRPC_PROPERTY = "jsonrpc";
		private const string METHOD_PROPERTY = "method";
		private const string ID_PROPERTY = "id";
		private const string PARAMS_PROPERTY = "params";

		private const string JSONRPC_VALUE = "2.0";

		public abstract string Method { get; }

		public string Id { get; set; }

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonUtils.Serialize(Serialize);
		}

		/// <summary>
		/// Serialize the instance to JSON.
		/// </summary>
		/// <param name="writer"></param>
		public void Serialize(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartObject();
			{
				// jsonrpc
				writer.WritePropertyName(JSONRPC_PROPERTY);
				writer.WriteValue(JSONRPC_VALUE);

				// method
				writer.WritePropertyName(METHOD_PROPERTY);
				writer.WriteValue(Method);

				// id
				if (!String.IsNullOrEmpty(Id))
				{
					writer.WritePropertyName(ID_PROPERTY);
					writer.WriteValue(Id);
				}

				// params
				writer.WritePropertyName(PARAMS_PROPERTY);
				SerializeParams(writer);
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected abstract void SerializeParams(JsonWriter writer);
	}
}
