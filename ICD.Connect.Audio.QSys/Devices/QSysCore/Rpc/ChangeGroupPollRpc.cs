using System;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public sealed class ChangeGroupPollRpc : AbstractRpc
    {
		private const string CHANGE_GROUP_ID_PROPERTY = "Id";

		private const string METHOD_VALUE = "ChangeGroup.Poll";

		private string ChangeGroupId { get; set; }

		public ChangeGroupPollRpc()
		{
		}

		public ChangeGroupPollRpc(ChangeGroup changeGroup)
		{
			ChangeGroupId = changeGroup.ChangeGroupId;
		}

		public override string Method { get { return METHOD_VALUE; } }

	    protected override void SerializeParams(JsonWriter writer)
	    {
		    if (writer == null)
			    throw new ArgumentNullException("writer");

		    // Name
		    writer.WritePropertyName(CHANGE_GROUP_ID_PROPERTY);
		    writer.WriteValue(ChangeGroupId);
	    }
    }
}
