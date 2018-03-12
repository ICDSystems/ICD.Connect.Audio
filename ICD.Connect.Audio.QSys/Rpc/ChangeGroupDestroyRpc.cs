﻿using System;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public sealed class ChangeGroupDestroyRpc : AbstractRpc
    {
		private const string CHANGE_GROUP_ID_PROPERTY = "Id";

		private const string METHOD_VALUE = "ChangeGroup.Destroy";

		private string ChangeGroupId { get; set; }

		public ChangeGroupDestroyRpc()
		{
		}

		public ChangeGroupDestroyRpc(ChangeGroup changeGroup)
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
