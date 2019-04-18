using ICD.Common.Utils.Extensions;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc":"2.0",
	///		"id":"3002",
	///		"method":"Snapshot.Load",
	///		"params":{
	///			"Name":"",
	///			"Bank":0,
	///			"Ramp":5
	///		}
	/// }
	/// </summary>
	public sealed class SnapshotLoadRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";
		private const string BANK_PROPERTY = "Bank";
		private const string RAMP_PROPERTY = "Ramp";

		private const string METHOD_VALUE = "Snapshot.Load";

		public override string Method { get { return METHOD_VALUE; } }

		public string Name { get; set; }

		public int Bank { get; set; }

		public int? Ramp { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			writer.WriteProperty(NAME_PROPERTY, Name);
			writer.WriteProperty(BANK_PROPERTY, Bank);
			writer.WriteProperty(RAMP_PROPERTY, Ramp);
		}
	}
}
