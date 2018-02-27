using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Controls;
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

	    public static IEnumerable<IDeviceControl> GetKrangControlsFromXml([NotNull] string xml, [NotNull] QSysCoreDevice qSysCore)
	    {
		    if (xml == null)
			    throw new ArgumentNullException("xml");
		    if (qSysCore == null)
			    throw new ArgumentNullException("qSysControl");

			List<IDeviceControl> controls = new List<IDeviceControl>();
		    foreach (string controlElement in XmlUtils.GetChildElementsAsString(xml))
		    {
			    int id = XmlUtils.GetAttributeAsInt(controlElement, "id");
				string name = XmlUtils.GetAttributeAsString(controlElement, "name");
			    string controlType = XmlUtils.GetAttributeAsString(controlElement, "type");
				
				// This is a quick method to get controls working for ConnectPro
				// todo: replace with proper reflection and device instantiation from XML
			    switch (controlType)
			    {
				    case ("NamedControlsVolumeDevice"):
				    {
					    controls.Add(GetNamedControlsVolumeDeviceFromXml(qSysCore, id, name, controlElement));
						break;
				    }
				    default:
				    {
					    qSysCore.Log(eSeverity.Error, "Failed to load control. No Control Matching type \"{0}\"", controlType);
					    continue;
				    }
			    }
				
		    }
		    return controls;
	    }

		/// <summary>
		/// todo: better error handling
		/// </summary>
		/// <param name="qSysCore"></param>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="xml"></param>
		/// <returns></returns>
	    private static IDeviceControl GetNamedControlsVolumeDeviceFromXml(QSysCoreDevice qSysCore, int id, string name, string xml)
	    {
		    int volumeId = XmlUtils.ReadChildElementContentAsInt(xml, "VolumeControlId");
		    int muteId = XmlUtils.ReadChildElementContentAsInt(xml, "MuteControlId");
			float? incrementValue = XmlUtils.TryReadChildElementContentAsFloat(xml, "IncrementValue");
			int? repeatBeforeTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBeforeTime");
			int? repeatBetweenTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBetweenTime");

		    INamedControl volumeControl = qSysCore.GetNamedControlById(volumeId);
			if (volumeControl == null)
				throw new KeyNotFoundException(String.Format("QSys - No Volume Control {0}", volumeId));
		    BooleanNamedControl muteControl = qSysCore.GetNamedControlById(muteId) as BooleanNamedControl;
		    if (muteControl == null)
			    throw new KeyNotFoundException(String.Format("QSys - No Mute Control {0}", muteId));

			NamedControlsVolumeDevice device = new NamedControlsVolumeDevice(qSysCore, name, id, volumeControl, muteControl);

			if (incrementValue != null)
				device.IncrementValue = (float)incrementValue;
			if (repeatBeforeTime != null)
				device.RepeatBeforeTime = (int)repeatBeforeTime;
			if (repeatBetweenTime != null)
				device.RepeatBetweenTime = (int)repeatBetweenTime;

			return device;
	    }
    }
}
