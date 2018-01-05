using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public sealed class LogonRpc : AbstractRpc
	{
		private const string USER_PROPERTY = "User";
		private const string PASSWORD_PROPERTY = "Password";

		private const string METHOD_VALUE = "Logon";

		public string Username { get; set; }

		public string Password { get; set; }

		public override string Method { get { return METHOD_VALUE; } }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			writer.WriteStartObject();
			{
				// Username
				writer.WritePropertyName(USER_PROPERTY);
				writer.WriteValue(Username);

				// Password
				writer.WritePropertyName(PASSWORD_PROPERTY);
				writer.WriteValue(Password);
			}
			writer.WriteEndObject();
		}
	}
}
