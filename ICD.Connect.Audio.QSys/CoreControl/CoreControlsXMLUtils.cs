using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControl.NamedComponent;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.CoreControl
{
    internal static class CoreControlsXmlUtils
    {
	    private static ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		public static IEnumerable<AbstractNamedControl> GetNamedControlsFromXml(string xml, QSysCoreDevice qSysCore)
	    {
			if (qSysCore == null)
			    throw new ArgumentNullException("qSysCore");

		    // First build a map of id to control elements
		    List<AbstractNamedControl> controls = new List<AbstractNamedControl>();
		    foreach (string controlElement in XmlUtils.GetChildElementsAsString(xml))
		    {
			    int id = XmlUtils.GetAttributeAsInt(controlElement, "id");
			    string name = XmlUtils.GetAttributeAsString(controlElement, "name");
			    string controlName = XmlUtils.GetAttributeAsString(controlElement, "controlName");
			    string controlType = XmlUtils.GetAttributeAsString(controlElement, "controlType");
			    int? changeGroup = null;
			    try
			    {
				    changeGroup = XmlUtils.GetAttributeAsInt(controlElement, "changeGroup");
			    }
			    catch(FormatException e)
			    {
			    }

			    AbstractNamedControl control = null;

			    switch (controlType.ToLower())
			    {
					case "namedcontrol":
						control = new NamedControl.NamedControl(qSysCore, id, name, controlName);
						break;
					case "booleannamedcontrol":
						control = new NamedControl.BooleanNamedControl(qSysCore, id, name, controlName);
						break;
					default:
						Logger.AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", controlType);
						continue;
						break;
				}
			    if (changeGroup != null)
				    qSysCore.AddNamedControlToChangeGroupById((int)changeGroup, control);
			    controls.Add(control);
		    }

		    return controls;
	    }

	    public static IEnumerable<INamedComponent> GetNamedComponentsFromXml(string namedComponentsXml, QSysCoreDevice qSysCoreDevice)
	    {
		    throw new NotImplementedException();
	    }

	    public static IEnumerable<ChangeGroup.ChangeGroup> GetChangeGroupsFromXml(string xml, QSysCoreDevice qSysCore)
	    {
			if (qSysCore == null)
			    throw new ArgumentNullException("qSysCore");

		    // First build a map of id to control elements
		    List<ChangeGroup.ChangeGroup> changeGroups = new List<ChangeGroup.ChangeGroup>();
		    foreach (string controlElement in XmlUtils.GetChildElementsAsString(xml))
		    {
			    int id = XmlUtils.GetAttributeAsInt(controlElement, "id");
			    string name = XmlUtils.GetAttributeAsString(controlElement, "name");
			    string changeGroupId = XmlUtils.GetAttributeAsString(controlElement, "changeGroupId");
			    float? pollInterval = null;
			    try
			    {
				    pollInterval = float.Parse(XmlUtils.GetAttributeAsString(controlElement, "pollInterval"));
			    }
				catch (FormatException e)
				{ }
				changeGroups.Add(new ChangeGroup.ChangeGroup(qSysCore, id, name, changeGroupId, pollInterval));
		    }

		    return changeGroups;
	    }
    }
}
