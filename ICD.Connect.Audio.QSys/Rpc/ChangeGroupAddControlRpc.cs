using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Connect.Audio.QSys.CoreControl.ChangeGroup;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Rpc
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

	    public ChangeGroupAddControlRpc(ChangeGroup changeGroup, AbstractNamedControl namedControl)
	    {
		    ChangeGroupId = changeGroup.ChangeGroupId;
		    m_Controls = new List<string> {namedControl.ControlName};
	    }

	    public ChangeGroupAddControlRpc(ChangeGroup changeGroup, IEnumerable<AbstractNamedControl> namedControls)
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
