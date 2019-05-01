using ICD.Common.Utils.Extensions;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public abstract class AbstractSnapshotRpc : AbstractRpc
	{
		private const string NAME_PROPERTY = "Name";
		private const string BANK_PROPERTY = "Bank";

		public string Name { get; set; }

		public int Bank { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			writer.WriteProperty(NAME_PROPERTY, Name);
			writer.WriteProperty(BANK_PROPERTY, Bank);
		}
	}
}
