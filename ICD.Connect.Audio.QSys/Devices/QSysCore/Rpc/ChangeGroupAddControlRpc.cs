﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public sealed class ChangeGroupAddControlRpc : AbstractRpc
    {
	    
	    private const string CHANGE_GROUP_ID_PROPERTY = "Id";
	    private const string CONTROLS_PROPERTY = "Controls";

	    private const string METHOD_VALUE = "ChangeGroup.AddControl";

	    private string ChangeGroupId { get; set; }
	    private readonly List<string> m_Controls;

	    public ChangeGroupAddControlRpc()
	    {
		    m_Controls = new List<string>();
	    }

	    public ChangeGroupAddControlRpc(ChangeGroup changeGroup, INamedControl namedControl)
	    {
		    ChangeGroupId = changeGroup.ChangeGroupId;
		    m_Controls = new List<string> {namedControl.ControlName};
	    }

	    public ChangeGroupAddControlRpc(ChangeGroup changeGroup, IEnumerable<INamedControl> namedControls)
	    {
		    ChangeGroupId = changeGroup.ChangeGroupId;
		    m_Controls = new List<string>(namedControls.Select(c => c.ControlName));
	    }

	    public override string Method { get { return METHOD_VALUE; } }

	    protected override void SerializeParams(JsonWriter writer)
	    {
		    if (writer == null)
			    throw new ArgumentNullException("writer");

		    // Name
		    writer.WritePropertyName(CHANGE_GROUP_ID_PROPERTY);
		    writer.WriteValue(ChangeGroupId);

		    // Controls
		    writer.WritePropertyName(CONTROLS_PROPERTY);
		    writer.WriteStartArray();
		    {
			    foreach (string control in m_Controls)
			    {
				    writer.WriteValue(control);
			    }
		    }
		    writer.WriteEndArray();
	    }
    }
}
