#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc":"2.0",
	///		"id":"3002",
	///		"method":"Snapshot.Load",
	///		"params":{
	///			"Name":"Camera1 Presets",
	///			"Bank":1,
	///			"Ramp":5
	///		}
	/// }
	/// </summary>
	public sealed class SnapshotLoadRpc : AbstractSnapshotRpc
	{
		private const string RAMP_PROPERTY = "Ramp";

		private const string METHOD_VALUE = "Snapshot.Load";

		public override string Method { get { return METHOD_VALUE; } }

		public int? Ramp { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			base.SerializeParams(writer);

			writer.WriteProperty(RAMP_PROPERTY, Ramp);
		}
	}
}
