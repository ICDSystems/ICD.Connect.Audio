﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Controls;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.CoreControls
{
	internal static class CoreElementsXmlUtils
	{
		private delegate object ImplicitControlFactory(int id, CoreElementsLoadContext context, string componentName);

		private delegate object ExplicitControlFactory(int id, string friendlyName, CoreElementsLoadContext context, string xml);

		private static Dictionary<Type, ImplicitControlFactory> m_ImpicitControlFactories;

		private static Dictionary<Type, ExplicitControlFactory> m_ExplicitControlFactories;

		private static ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		/// <summary>
		/// This is the method called by the core to load the controls
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="qSysCore"></param>
		/// <returns></returns>
		public static CoreElementsLoadContext GetControlsFromXml(string xml, QSysCoreDevice qSysCore)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (qSysCore == null)
				throw new ArgumentNullException("qSysCore");

			CoreElementsLoadContext loadContext = new CoreElementsLoadContext(qSysCore);

			// Load attributes into dictionary for easier lookup
			Dictionary<string, IcdXmlAttribute> attributes = XmlUtils.GetAttributes(xml).ToDictionary(attribute => attribute.Name);

			// Load Id's and Types To continue in proper order
			foreach (string elementXml in XmlUtils.GetChildElementsAsString(xml))
			{
				int id;
				string elementTypeString, elementNameString;
				Type elementType;
				try
				{
					id = XmlUtils.GetAttributeAsInt(elementXml, "id");
					elementTypeString = XmlUtils.GetAttributeAsString(elementXml, "type");
					elementNameString = XmlUtils.GetAttributeAsString(elementXml, "name");
					elementType = GetTypeForText(elementTypeString);
				}
				catch (IcdXmlException e)
				{
					qSysCore.Log(eSeverity.Error, e, "Error parsing XML for element");
					continue;
				}

				if (elementType != null)
					loadContext.AddElement(id, elementType, elementNameString, elementXml);
				else
					loadContext.QSysCore.Log(eSeverity.Error, "No type matching {0} for element id {1}", elementTypeString,
					                         id);
			}


			// Setup ChangeGroups
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => typeof(IChangeGroup).IsAssignableFrom(p.Value)))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);

				IChangeGroup changeGroup = ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext, controlXml) as IChangeGroup;

				if (changeGroup == null)
					continue;

				loadContext.AddChangeGroup(changeGroup);
			}

			// Setup Default Change Group
			IcdXmlAttribute defaultChangeGroup;
			if (attributes.TryGetValue("DefaulthangeGroup", out defaultChangeGroup))
			{
				int defaultChangeGroupId = int.Parse(defaultChangeGroup.Value);
				if (typeof(IChangeGroup).IsAssignableFrom(loadContext.GetTypeForId(defaultChangeGroupId)))
					loadContext.AddDefaultChangeGroup(defaultChangeGroupId);
				else
					loadContext.QSysCore.Log(eSeverity.Error,
					                         "Tried to add DefaultChangeGroup {0}, but there is no change group with that ID.",
					                         defaultChangeGroup);
			}

			// Is Auto Change Group Disabled?
			bool autoChangeGroupEnabled = true;
			IcdXmlAttribute autoChangeGroupAttribute;
			if (attributes.TryGetValue("DisableAutoChangeGroup", out autoChangeGroupAttribute))
				autoChangeGroupEnabled = !(bool.Parse(autoChangeGroupAttribute.Value));
			// Setup Auto Change Group
			if (autoChangeGroupEnabled)
			{
				int autoChangeGroupId = loadContext.GetNextId();
				loadContext.AddElement(autoChangeGroupId, typeof(ChangeGroup), "Auto Change Group", null);
				IChangeGroup autoChangeGroup = ReflectionUtils.CreateInstance(typeof(ChangeGroup), autoChangeGroupId, loadContext, "AutoChangeGroup") as IChangeGroup;
				loadContext.AddChangeGroup(autoChangeGroup);
				loadContext.AddDefaultChangeGroup(autoChangeGroupId);
			}

			// Setup Named Controls
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => typeof(INamedControl).IsAssignableFrom(p.Value)))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);
				
				INamedControl control = ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key),loadContext, controlXml) as INamedControl;

				if (control == null)
					continue;

				loadContext.AddNamedControl(control);
			}

			// Setup Named Components
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => typeof(INamedComponent).IsAssignableFrom(p.Value)))
			{
				string componentXml = loadContext.GetXmlForElementId(kvp.Key);

				INamedComponent component = ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext, componentXml) as INamedComponent;

				if (component == null)
					continue;

				loadContext.AddNamedComponent(component);
			}

			// Setup Krang Controls
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => typeof(IQSysKrangControl).IsAssignableFrom(p.Value)))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);

				IQSysKrangControl control = ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext, controlXml) as IQSysKrangControl;

				if (control == null)
					continue;

				loadContext.AddKrangControl(control);
			}

			return loadContext;
		}

		private static Type GetTypeForText(string typeText)
		{
			switch (typeText)
			{
				case "NamedControlVolume":
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
				case "VoIPComponent":
				{
					return typeof(VoipNamedComponent);
				}
				case "VoIPComponentControl":
				{
					return typeof(VoipComponentDialingDevice);
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