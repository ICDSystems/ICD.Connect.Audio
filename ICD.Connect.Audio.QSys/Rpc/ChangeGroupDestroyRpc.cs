﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Connect.Audio.QSys.CoreControl.ChangeGroup;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
{
    class ChangeGroupDestroyRpc : AbstractRpc
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

			writer.WriteStartObject();
			{
				// Name
				writer.WritePropertyName(CHANGE_GROUP_ID_PROPERTY);
				writer.WriteValue(ChangeGroupId);
			}
			writer.WriteEndObject();
		}
	}
}
