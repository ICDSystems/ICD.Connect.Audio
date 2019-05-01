namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc":"2.0",
	///		"method":"Snapshot.Save",
	///		"params":{
	///			"Name":"Camera1 Presets",
	///			"Bank":1
	///		}
	/// }
	/// </summary>
	public sealed class SnapshotSaveRpc : AbstractSnapshotRpc
	{
		private const string METHOD_VALUE = "Snapshot.Save";

		public override string Method { get { return METHOD_VALUE; } }
	}
}
