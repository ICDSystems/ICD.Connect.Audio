using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using Newtonsoft.Json;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public sealed class ChangeGroupAddComponentControlRpc : AbstractRpc
    {
	    
	    private const string CHANGE_GROUP_ID_PROPERTY = "Id";
		private const string COMPONENT_PROPERTY = "Component";
	    private const string CONTROLS_PROPERTY = "Controls";
		private const string NAME_PROPERTY = "Name";

	    private const string METHOD_VALUE = "ChangeGroup.AddComponentControl";

	    private string ChangeGroupId { get; set; }
		private string ComponentName { get; set; }
	    private List<string> ControlNames { get; set; } 

	    public ChangeGroupAddComponentControlRpc()
	    {
		    ControlNames = new List<string>();
	    }

	    public ChangeGroupAddComponentControlRpc(ChangeGroup changeGroup, INamedComponent namedComponent)
	    {
		    ChangeGroupId = changeGroup.ChangeGroupId;
		    ComponentName = namedComponent.ComponentName;
			ControlNames = new List<string>(namedComponent.GetControls().Select(c => c.Name));
	    }

		public ChangeGroupAddComponentControlRpc(ChangeGroup changeGroup, INamedComponent namedComponent,
		                                         IEnumerable<INamedComponentControl> componentControls)
		{
			ChangeGroupId = changeGroup.ChangeGroupId;
			ComponentName = namedComponent.ComponentName;
			ControlNames = new List<string>(componentControls.Select(c => c.Name));
		}

	    public override string Method { get { return METHOD_VALUE; } }

	    protected override void SerializeParams(JsonWriter writer)
	    {
		    if (writer == null)
			    throw new ArgumentNullException("writer");

		    // Name
		    writer.WritePropertyName(CHANGE_GROUP_ID_PROPERTY);
		    writer.WriteValue(ChangeGroupId);

		    // Component
		    writer.WritePropertyName(COMPONENT_PROPERTY);
			writer.WriteStartObject();
		    {
				// Component Name
			    writer.WritePropertyName(NAME_PROPERTY);
				writer.WriteValue(ComponentName);

				// Component Controls
				writer.WritePropertyName(CONTROLS_PROPERTY);
				writer.WriteStartArray();
			    {
				    foreach (string control in ControlNames)
				    {
					    writer.WriteStartObject();
					    {
							writer.WritePropertyName(NAME_PROPERTY);
							writer.WriteValue(control);
					    }
						writer.WriteEndObject();
				    }
			    }
				writer.WriteEndArray();
		    }
			writer.WriteEndObject();
	    }
    }
}
