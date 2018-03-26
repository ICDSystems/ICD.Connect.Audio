using System;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public sealed class ChangeGroupAutoPollRpc : AbstractRpc
    {
		private const string CHANGE_GROUP_ID_PROPERTY = "Id";
		private const string CHANGE_GROUP_POLL_INTERVAL_PROPERTY = "Rate";

		private const string METHOD_VALUE = "ChangeGroup.AutoPoll";

	    public override string Method { get { return METHOD_VALUE; } }
		public override string Id { get { return RpcUtils.RPCID_CHANGEGROUP_RESPONSE; } }

	    private string ChangeGroupId { get; set; }
		private float ChangeGroupPollInterval { get; set; }

		public ChangeGroupAutoPollRpc()
		{
		}

		public ChangeGroupAutoPollRpc(ChangeGroup changeGroup)
		{
			if (changeGroup.PollInterval == null)
				throw new InvalidOperationException("Can't send null poll interval for change group");

			ChangeGroupId = changeGroup.ChangeGroupId;
			ChangeGroupPollInterval = (float)changeGroup.PollInterval;
		}

	    protected override void SerializeParams(JsonWriter writer)
	    {
		    if (writer == null)
			    throw new ArgumentNullException("writer");


		    // Name
		    writer.WritePropertyName(CHANGE_GROUP_ID_PROPERTY);
		    writer.WriteValue(ChangeGroupId);

		    // Rate
		    writer.WritePropertyName(CHANGE_GROUP_POLL_INTERVAL_PROPERTY);
		    writer.WriteValue(ChangeGroupPollInterval);

	    }
    }
}
