using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Dialing;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Partitioning;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Volume;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Devices.Utils;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls
{
	internal static class CoreElementsXmlUtils
	{
		private static readonly BiDictionary<Type, string> s_TypeToAttribute = new BiDictionary<Type, string>
		{
			// Change group
			{typeof(ChangeGroup), "ChangeGroup"},

			// Named controls
			{typeof(NamedControl), "NamedControl"},
			{typeof(BooleanNamedControl), "BooleanNamedControl"},

			// Named components
			{typeof(CameraNamedComponent), "CameraComponent"},
			{typeof(PotsNamedComponent), "PotsComponent"},
			{typeof(VoipNamedComponent), "VoIPComponent"},
			{typeof(SnapshotNamedComponent), "SnapshotComponent"},
			{typeof(AudioSwitcherNamedComponent), "AudioSwitcherComponent"},
			{typeof(CameraSwitcherNamedComponent), "CameraSwitcherComponent"},

			// Krang controls
			{typeof(QSysVolumePercentControl), "NamedControlVolume"},
			{typeof(QSysPrivacyMuteControl), "NamedControlPrivacy"},
			{typeof(QSysPotsTraditionalConferenceControl), "PotsComponentControl"},
			{typeof(QSysVoipTraditionalConferenceControl), "VoIPComponentControl"},
			{typeof(QSysPartitionControl), "NamedControlPartition"},
		};

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
			Dictionary<string, string> attributes = XmlUtils.GetAttributes(xml).ToDictionary();

			// Load Id's and Types To continue in proper order
			foreach (string elementXml in XmlUtils.GetChildElementsAsString(xml))
			{
				int id;
				string elementTypeString, elementNameString;
				Type elementType;
				Guid uuid;

				try
				{
					id = XmlUtils.GetAttributeAsInt(elementXml, "id");
					elementTypeString = XmlUtils.GetAttributeAsString(elementXml, "type");
					elementNameString = XmlUtils.GetAttributeAsString(elementXml, "name");

					try
					{
						uuid = XmlUtils.GetAttributeAsGuid(elementXml, "uuid");
					}
					catch (Exception)
					{
						uuid = DeviceControlUtils.GenerateUuid(qSysCore, id);
					}

					s_TypeToAttribute.TryGetKey(elementTypeString, out elementType);
				}
				catch (IcdXmlException e)
				{
					qSysCore.Logger.Log(eSeverity.Error, e, "Error parsing XML for element");
					continue;
				}

				if (elementType != null)
					loadContext.AddElement(id, uuid, elementType, elementNameString, elementXml);
				else
					loadContext.QSysCore.Logger.Log(eSeverity.Error, "No type matching {0} for element id {1}", elementTypeString,
					                         id);
			}

			SetupChangeGroups(loadContext, attributes);
			SetupNamedControls(loadContext);
			SetupNamedComponents(loadContext);
			SetupKrangControls(loadContext);

			return loadContext;
		}

		private static void SetupChangeGroups(CoreElementsLoadContext loadContext, IDictionary<string, string> attributes)
		{
			// Setup ChangeGroups
			foreach (
				KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => p.Value.IsAssignableTo<IChangeGroup>()))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);

				IChangeGroup changeGroup;

				try
				{
					changeGroup =
						ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext,
						                               controlXml) as IChangeGroup;
				}
				catch (Exception e)
				{
					loadContext.QSysCore.Logger.Log(eSeverity.Error, e, "Failed to create ChangeGroup {0} - {1}", kvp.Key, e.Message);
					continue;
				}

				loadContext.AddChangeGroup(changeGroup);
			}

			// Setup Default Change Group
			string defaultChangeGroup;
			if (attributes.TryGetValue("DefaultChangeGroup", out defaultChangeGroup))
			{
				int defaultChangeGroupId = int.Parse(defaultChangeGroup);
				if (typeof(IChangeGroup).IsAssignableFrom(loadContext.GetTypeForId(defaultChangeGroupId)))
					loadContext.AddDefaultChangeGroup(defaultChangeGroupId);
				else
					loadContext.QSysCore.Logger.Log(eSeverity.Error,
					                         "Tried to add DefaultChangeGroup {0}, but there is no change group with that ID.",
					                         defaultChangeGroup);
			}

			// Is Auto Change Group Disabled?
			bool autoChangeGroupEnabled = true;
			string autoChangeGroupAttribute;
			if (attributes.TryGetValue("DisableAutoChangeGroup", out autoChangeGroupAttribute))
				autoChangeGroupEnabled = !bool.Parse(autoChangeGroupAttribute);

			// Setup Auto Change Group
			if (!autoChangeGroupEnabled)
				return;

			int autoChangeGroupId = loadContext.GetNextId();
			loadContext.AddElement(autoChangeGroupId, Guid.Empty, typeof(ChangeGroup), "Auto Change Group", null);

			IChangeGroup autoChangeGroup = null;

			try
			{
				autoChangeGroup = new ChangeGroup(autoChangeGroupId, loadContext, "AutoChangeGroup");
			}
			catch (Exception e)
			{
				loadContext.QSysCore.Logger.Log(eSeverity.Error, e, "Failed to create ChangeGroup {0} - {1}", autoChangeGroupId, e.Message);
			}

			loadContext.AddChangeGroup(autoChangeGroup);
			loadContext.AddDefaultChangeGroup(autoChangeGroupId);
		}

		private static void SetupNamedControls(CoreElementsLoadContext loadContext)
		{
			// Setup Named Controls
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => p.Value.IsAssignableTo<INamedControl>()))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);

				INamedControl control;

				try
				{
					control =
						ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext,
						                               controlXml) as INamedControl;
				}
				catch (Exception e)
				{
					loadContext.QSysCore.Logger.Log(eSeverity.Error, e, "Failed to create NamedControl {0} - {1}", kvp.Key, e.Message);
					continue;
				}

				loadContext.AddNamedControl(control);
			}
		}

		private static void SetupNamedComponents(CoreElementsLoadContext loadContext)
		{
			// Setup Named Components
			foreach (KeyValuePair<int, Type> kvp in loadContext.GetElementsTypes().Where(p => p.Value.IsAssignableTo<INamedComponent>()))
			{
				string componentXml = loadContext.GetXmlForElementId(kvp.Key);

				INamedComponent component;

				try
				{
					component =
						ReflectionUtils.CreateInstance(kvp.Value, kvp.Key, loadContext.GetNameForElementId(kvp.Key), loadContext,
						                               componentXml) as INamedComponent;
				}
				catch (Exception e)
				{
					loadContext.QSysCore.Logger.Log(eSeverity.Error, e, "Failed to create NamedComponent {0} - {1}", kvp.Key, e.Message);
					continue;
				}

				loadContext.AddNamedComponent(component);
			}
		}

		private static void SetupKrangControls(CoreElementsLoadContext loadContext)
		{
			// Setup Krang Controls
			foreach (
				KeyValuePair<int, Type> kvp in
					loadContext.GetElementsTypes().Where(p => typeof(IQSysKrangControl).IsAssignableFrom(p.Value)))
			{
				string controlXml = loadContext.GetXmlForElementId(kvp.Key);

				IQSysKrangControl control;

				try
				{
					control =
						ReflectionUtils.CreateInstance(kvp.Value,
						                               kvp.Key,
													   loadContext.GetUuidForElementId(kvp.Key),
						                               loadContext.GetNameForElementId(kvp.Key),
						                               loadContext,
						                               controlXml) as IQSysKrangControl;
				}
				catch (Exception e)
				{
					loadContext.QSysCore.Logger.Log(eSeverity.Error, e, "Failed to create QsysKrangControl {0} - {1}", kvp.Key, e.Message);
					continue;
				}

				loadContext.AddKrangControl(control);
			}
		}
	}
}
