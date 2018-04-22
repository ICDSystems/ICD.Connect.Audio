using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Controls;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Devices.Controls;
using BindingFlags = System.Reflection.BindingFlags;

namespace ICD.Connect.Audio.QSys.CoreControls
{
	internal static class CoreControlsXmlUtils
	{
		private static ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		private static NamedControl GetNamedControlFromXml(QSysCoreDevice qSysCore, int id, string name,
		                                                   string xml)
		{
			if (qSysCore == null)
				throw new ArgumentNullException("qSysCore");

			string controlName = XmlUtils.GetAttributeAsString(xml, "controlName");
			int? changeGroup = null;
			try
			{
				changeGroup = XmlUtils.GetAttributeAsInt(xml, "changeGroup");
			}
			catch (FormatException e)
			{
			}

			NamedControl control = new NamedControl(qSysCore, id, name, controlName);
			if (changeGroup != null)
				qSysCore.AddNamedControlToChangeGroupById((int)changeGroup, control);

			return control;
		}

		private static BooleanNamedControl GetBooleanNamedControlFromXml(QSysCoreDevice qSysCore, int id, string name,
		                                                                 string xml)
		{
			if (qSysCore == null)
				throw new ArgumentNullException("qSysCore");

			string controlName = XmlUtils.GetAttributeAsString(xml, "controlName");
			int? changeGroup = null;
			try
			{
				changeGroup = XmlUtils.GetAttributeAsInt(xml, "changeGroup");
			}
			catch (FormatException e)
			{
			}

			BooleanNamedControl control = new BooleanNamedControl(qSysCore, id, name, controlName);
			if (changeGroup != null)
				qSysCore.AddNamedControlToChangeGroupById((int)changeGroup, control);

			return control;
		}

		private static ChangeGroup GetChangeGroupControlFromXml(QSysCoreDevice qSysCore, int id, string name, string xml)
		{
			if (qSysCore == null)
				throw new ArgumentNullException("qSysCore");

			string changeGroupId = XmlUtils.GetAttributeAsString(xml, "changeGroupId");
			float? pollInterval = null;
			try
			{
				pollInterval = float.Parse(XmlUtils.GetAttributeAsString(xml, "pollInterval"));
			}
			catch (FormatException e)
			{
			}

			return new ChangeGroup(qSysCore, id, name, changeGroupId, pollInterval);
		}

		/// <summary>
		/// todo: better error handling
		/// </summary>
		/// <param name="qSysCore"></param>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="xml"></param>
		/// <returns></returns>
		private static IDeviceControl GetNamedControlsVolumeDeviceControlFromXml(
			QSysCoreDevice qSysCore, int id, string name, string xml)
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

		public static IEnumerable<IDeviceControl> GetControlsFromXml(string xml, QSysCoreDevice qSysCore)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (qSysCore == null)
				throw new ArgumentNullException("qSysCore");

			CoreControlsLoadContext loadContext = new CoreControlsLoadContext(qSysCore);

			List<IDeviceControl> controls = new List<IDeviceControl>();

			// Load Id's and Types To continue in proper order
			foreach (string controlXml in XmlUtils.GetChildElementsAsString(xml))
			{
				int id = XmlUtils.GetAttributeAsInt(controlXml, "id");

				string controlTypeString = XmlUtils.GetAttributeAsString(controlXml, "type");

				Type controlType = GetTypeForText(controlTypeString);

				if (controlType != null)
					loadContext.AddControl(id, controlType, controlXml);
				else
					loadContext.QSysCore.Log(eSeverity.Error, "No control type matching type {0} for control id {1}", controlTypeString, id);
			}

			// Setup Default Change Group
			int defaultChangeGroup;
			try
			{
				defaultChangeGroup = XmlUtils.GetAttributeAsInt(xml, "DefaultChangeGroup");
			}
			catch (IcdXmlException)
			{
				defaultChangeGroup = 0;
			}
			if (defaultChangeGroup != 0)
			{
				if (loadContext.GetTypeForId(defaultChangeGroup) == typeof(ChangeGroup))
					loadContext.AddDefaultChangeGroup(defaultChangeGroup);
				else
					loadContext.QSysCore.Log(eSeverity.Error,
					                         "Tried to add DefaultChangeGroup {0}, but there is no change group with that ID.",
					                         defaultChangeGroup);
			}

			// Is Auto Change Group Disabled?
			bool autoChangeGroup;
			try
			{
				autoChangeGroup = !XmlUtils.GetAttributeAsBool(xml, "DisableAutoChangeGroup");
			}
			catch (IcdXmlException)
			{
				autoChangeGroup = true;
			}
			// Setup Auto Change Group
			if (autoChangeGroup)
			{
				int autoChangeGroupId = loadContext.GetNextId();
				loadContext.AddControl(autoChangeGroupId, typeof(ChangeGroup), null);
				loadContext.AddDefaultChangeGroup(autoChangeGroupId);
			}

			// Setup Named Controls
			foreach (KeyValuePair<int, Type> kvp in loadContext.ControlsTypes.Where((p) => typeof(INamedControl).IsAssignableFrom(p.Value)))
			{
				INamedControl control;
				string controlXml = loadContext.GetXmlForId(kvp.Key);
				
				if (controlXml != null)
					control = ReflectionUtils.CreateInstance(kvp.Value, loadContext, controlXml) as INamedControl;
				else
					//todo: Create named control from control without XML?
					control = null;

				if (control == null)
					continue;

				loadContext.LinkNamedControl(control.ControlName, kvp.Key);
				controls.Add(control);
			}

			// Setup Named Components
			foreach (KeyValuePair<int, Type> kvp in loadContext.ControlsTypes.Where((p) => typeof(INamedComponent).IsAssignableFrom(p.Value)))
			{
				INamedComponent component;
				string componentXml = loadContext.GetXmlForId(kvp.Key);

				if (componentXml != null)
					component = ReflectionUtils.CreateInstance(kvp.Value, loadContext, componentXml) as INamedComponent;
				else
					//todo: Create named control from control without XML?
					component = null;

				if (component == null)
					continue;

				loadContext.LinkNamedComponent(component.ComponentName, kvp.Key);
				controls.Add(component);
			}


			return controls;
		}

		private static Type GetTypeForText(string typeText)
		{
			switch (typeText)
			{
				case "NamedControlsVolumeDeviceControl":
				{
					return typeof(NamedControlsVolumeDevice);
				}
				case "ChangeGroup":
				{
					return typeof(ChangeGroup);
				}
				case "NamedControl":
				{
					return typeof(NamedControl);
				}
				case "BooleanNamedContol":
				{
					return typeof(BooleanNamedControl);
				}
				default:
				{
					Logger.AddEntry(eSeverity.Error, "QSys Failed to load control. No Control Matching type \"{0}\"", typeText);
					break;
				}
			}

			return null;
		}

	}

}
