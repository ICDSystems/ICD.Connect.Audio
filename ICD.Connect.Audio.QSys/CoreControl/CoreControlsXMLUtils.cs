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
    class CoreControlsXmlUtils
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

			    switch (controlType.ToLower())
			    {
					case "namedcontrol":
						controls.Add(new NamedControl.NamedControl(qSysCore, id, name, controlName));
						break;
					case "booleannamedcontrol":
						controls.Add(new NamedControl.BooleanNamedControl(qSysCore, id, name, controlName));
						break;
					default:
						Logger.AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", controlType);
						break;
				}
		    }

		    return controls;
	    }

	    public static IEnumerable<INamedComponent> GetNamedComponentsFromXml(string namedComponentsXml, QSysCoreDevice qSysCoreDevice)
	    {
		    throw new NotImplementedException();
	    }
    }
}
