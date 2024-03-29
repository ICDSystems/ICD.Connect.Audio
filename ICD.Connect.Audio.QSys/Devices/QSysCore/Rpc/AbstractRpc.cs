﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public abstract class AbstractRpc : IRpc
	{
		private const string JSONRPC_PROPERTY = "jsonrpc";
		private const string METHOD_PROPERTY = "method";
		private const string ID_PROPERTY = "id";
		private const string PARAMS_PROPERTY = "params";

		private const string JSONRPC_VALUE = "2.0";

		public abstract string Method { get; }

		public virtual string Id { get; set; }

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
				if (!string.IsNullOrEmpty(Id))
				{
					writer.WritePropertyName(ID_PROPERTY);
					writer.WriteValue(Id);
				}

				// params
				writer.WritePropertyName(PARAMS_PROPERTY);
				WriteParamsContainer(writer);
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected abstract void SerializeParams(JsonWriter writer);

		/// <summary>
		/// Override to write the object/array for the params
		/// </summary>
		/// <param name="writer"></param>
		protected virtual void WriteParamsContainer(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartObject();
			{
				SerializeParams(writer);
			}
			writer.WriteEndObject();
		}
	}
}
