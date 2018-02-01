using System;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	/// <summary>
	/// {
	///		"jsonrpc": "2.0",
	///		"id": 1234,
	///		"method": "Control.Set",
	///		"params": {
	///			"Name": "MainGain",
	///			"Position": -12
	///		}
	/// }
	/// </summary>
	public sealed class ControlSetPositionRpc : AbstractControlSetRpc
	{
		private const string POSITION_PROPERTY = "Position";

		public float Position { get; set; }

		/// <summary>
		/// Override to add serialize params to JSON.
		/// </summary>
		/// <param name="writer"></param>
		protected override void SerializeParams(JsonWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			base.SerializeParams(writer);

			// Control position
			writer.WritePropertyName(POSITION_PROPERTY);
			writer.WriteValue(Position);
		}

		public ControlSetPositionRpc(INamedControl control, float position) : base(control)
	    {
		    Position = position;
	    }
	}
}
