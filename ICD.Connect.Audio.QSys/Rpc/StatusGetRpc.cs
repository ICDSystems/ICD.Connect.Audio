using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public sealed class StatusGetRpc : AbstractRpc
	{
		private const string METHOD_VALUE = "StatusGet";

		public override string Method { get { return METHOD_VALUE; } }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			writer.WriteValue(0);
		}
	}
}
