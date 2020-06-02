using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls
{
	/// <summary>
	/// Context to load the core controls in
	/// Note: Not Thread Safe
	/// </summary>
	public sealed class CoreElementsLoadContext
    {
		#region Constants

	    private const int NEXT_ID_START = 1000;

		#endregion

		#region Fields

	    /// <summary>
	    /// Controls ID to Control Type
	    /// </summary>
	    private readonly Dictionary<int, Type> m_ElementsTypes;

	    /// <summary>
	    /// Element ID to UUID
	    /// </summary>
	    private readonly Dictionary<int, Guid> m_ElementUuids;

		/// <summary>
		/// Element ID to Friendly Name
		/// </summary>
		private readonly Dictionary<int, string> m_ElementNames;

		/// <summary>
		/// Element ID to XML String (null for implicit controls)
		/// </summary>
		private readonly Dictionary<int, string> m_ElementsXml;

		/// <summary>
		/// Binding Change Groups by QSys "Id" to control id
		/// </summary>
		private readonly Dictionary<string, int> m_ChangeGroupBindings;

		/// <summary>
		/// Binding Named Controls by Qsys "Name" to control id
		/// </summary>
	    private readonly Dictionary<string, int> m_NamedControlBindings;

		/// <summary>
		/// Binding Named Components by Qsys "Name" to control id
		/// </summary>
	    private readonly Dictionary<string, int> m_NamedComponentBindings;

		/// <summary>
		/// Change Group Objects by id
		/// </summary>
		private readonly Dictionary<int, IChangeGroup> m_ChangeGroups;

		/// <summary>
		/// Named Control Objects by id
		/// </summary>
		private readonly Dictionary<int, INamedControl> m_NamedControls;

		/// <summary>
		/// Named Component Objects by id
		/// </summary>
		private readonly Dictionary<int, INamedComponent> m_NamedComponents;

		/// <summary>
		/// Krang Control Objects by id
		/// </summary>
		private readonly Dictionary<int, IQSysKrangControl> m_KrangControls;

		/// <summary>
		/// Next Available ID to try for a control
		/// </summary>
	    private int m_NextAvailableId;

		/// <summary>
		/// Default change groups for controls to subscribe to
		/// </summary>
	    private readonly List<int> m_DefaultChangeGroups;

		#endregion

		#region Properties

		/// <summary>
		/// Core for controls in load context
		/// </summary>
	    internal QSysCoreDevice QSysCore { get; private set; }

		#endregion

		#region Constructor

		public CoreElementsLoadContext(QSysCoreDevice qSysCore)
	    {
		    QSysCore = qSysCore;
			m_NextAvailableId = NEXT_ID_START;
			m_ElementsTypes = new Dictionary<int, Type>();
			m_ElementUuids = new Dictionary<int, Guid>();
			m_ElementNames = new Dictionary<int, string>();
			m_ElementsXml = new Dictionary<int, string>();
			m_ChangeGroupBindings = new Dictionary<string, int>();
			m_NamedControlBindings = new Dictionary<string, int>();
			m_NamedComponentBindings = new Dictionary<string, int>();
			m_ChangeGroups = new Dictionary<int, IChangeGroup>();
			m_NamedControls = new Dictionary<int, INamedControl>();
			m_NamedComponents = new Dictionary<int, INamedComponent>();
			m_KrangControls = new Dictionary<int, IQSysKrangControl>();
			m_DefaultChangeGroups = new List<int>();
	    }

		#endregion

		#region Methods

		#region Add Methods

		/// <summary>
		/// Adds a generic control
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="controlType"></param>
		/// <param name="controlName"></param>
		/// <param name="xml"></param>
		internal void AddElement(int id, Guid uuid, Type controlType, string controlName, string xml)
	    {
		    m_ElementsTypes[id] = controlType;
		    m_ElementsXml[id] = xml;
		    m_ElementUuids[id] = uuid;
			m_ElementNames[id] = controlName;
	    }

		/// <summary>
		/// Adds a change group to the change group collection
		/// </summary>
		/// <param name="changeGroup"></param>
		internal void AddChangeGroup(IChangeGroup changeGroup)
		{
			AddElement(changeGroup.Id, Guid.Empty, changeGroup.GetType(), changeGroup.Name, null);
			BindChangeGroup(changeGroup);
			m_ChangeGroups.Add(changeGroup.Id, changeGroup);
		}

		/// <summary>
		/// Adds Named Control to the collection and binds it
		/// </summary>
		/// <param name="namedControl"></param>
		internal void AddNamedControl(INamedControl namedControl)
		{
			AddElement(namedControl.Id, Guid.Empty, namedControl.GetType(), namedControl.Name, null);
			BindNamedControl(namedControl);
			m_NamedControls.Add(namedControl.Id, namedControl);
		}

		/// <summary>
		/// Adds Named Component to the collection and binds it
		/// </summary>
		/// <param name="namedComponent"></param>
		internal void AddNamedComponent(INamedComponent namedComponent)
		{
			AddElement(namedComponent.Id, Guid.Empty, namedComponent.GetType(), namedComponent.Name, null);
			BindNamedComponent(namedComponent);
			m_NamedComponents.Add(namedComponent.Id, namedComponent);
		}

		/// <summary>
		/// Adds Krang Control the the collection
		/// </summary>
		/// <param name="krangControl"></param>
		internal void AddKrangControl(IQSysKrangControl krangControl)
		{
			AddElement(krangControl.Id, krangControl.Uuid, krangControl.GetType(), krangControl.Name, null);
			m_KrangControls.Add(krangControl.Id, krangControl);
		}

		/// <summary>
		/// Adds the ID as a default change group
		/// </summary>
		/// <param name="id"></param>
		internal void AddDefaultChangeGroup(int id)
		{
			m_DefaultChangeGroups.Add(id);
		}

		#endregion

		#region Lazy Loaders

		[CanBeNull]
		public TControl LazyLoadNamedControl<TControl>(string controlName)
			where TControl : INamedControl
		{
			return (TControl)LazyLoadNamedControl(controlName, typeof(TControl));
		}

		[CanBeNull]
		public INamedControl LazyLoadNamedControl(string controlName, Type controlType)
		{
			if (!typeof(INamedControl).IsAssignableFrom(controlType))
				throw new ArgumentException("componentType must be a type that implements INamedControl.");

			INamedControl control = null;
			int id;

			if (m_NamedControlBindings.TryGetValue(controlName, out id))
			{
				control = m_NamedControls[id];
				if (controlType.IsInstanceOfType(control))
					return control;

				QSysCore.Logger.Log(eSeverity.Error, "Error Loading Config, Named Control {0} exists, but is not of type {1}",
				                    controlName,
				                    controlType);
				return null;
			}

			// If control was not found, instantiate an implicit control
			try
			{
				control = ReflectionUtils.CreateInstance(controlType, GetNextId(), this, controlName) as INamedControl;
			}
			catch (TypeLoadException e)
			{
				QSysCore.Logger.Log(eSeverity.Error, e, "Exception Instantiating Implicit Control Name:{0}, Type:{1} - Exception In Constructor.", controlName,
				             controlType);
				return null;
			}
			catch (InvalidOperationException e)
			{
				QSysCore.Logger.Log(eSeverity.Error, e, "Exception Instantiating Implicit Control Name:{0}, Type:{1} - Constructor Not Found.", controlName, controlType);
			}

			if (control == null)
			{
				QSysCore.Logger.Log(eSeverity.Error, "Error Instantiating Implicit Control Name:{0}, Type:{1}", controlName, controlType);
				return null;
			}
			AddNamedControl(control);

			return control;
		}

		[CanBeNull]
		public TComponent LazyLoadNamedComponent<TComponent>(string componentName)
			where TComponent : INamedComponent
		{
			return (TComponent)LazyLoadNamedComponent(componentName, typeof(TComponent));
		}

		[CanBeNull]
		public INamedComponent LazyLoadNamedComponent(string componentName, Type componentType)
		{
			if (!typeof(INamedComponent).IsAssignableFrom(componentType))
				throw new ArgumentException("componentType must be a type that implements INamedComponent.");

			INamedComponent component;
			int id;

			if (m_NamedComponentBindings.TryGetValue(componentName, out id))
			{
				component = m_NamedComponents[id];
				if (componentType.IsInstanceOfType(component))
					return component;

				QSysCore.Logger.Log(eSeverity.Error, "Error Loading Config, Named Component {0} exists, but is not of type {1}", componentName,
							 componentType);
				return null;
			}

			// If control was not found, instantiate an implicit control
			try
			{
				component = ReflectionUtils.CreateInstance(componentType, GetNextId(), this, componentName) as INamedComponent;
			}
			catch (TypeLoadException e)
			{
				QSysCore.Logger.Log(eSeverity.Error, e, "Exception Instantiating Implicit Component Name:{0}, Type:{1} - Exception In Constructor", componentName, componentType);
				return null;
			}
			catch (InvalidOperationException e)
			{
				QSysCore.Logger.Log(eSeverity.Error, e, "Exception Instantiating Implicit Component Name:{0}, Type:{1} - Constructor Not Found", componentName, componentType);
				return null;
			}

			if (component == null)
			{
				QSysCore.Logger.Log(eSeverity.Error, "Error Instantiating Implicit Component Name:{0}, Type:{1}", componentName, componentType);
				return null;
			}

			AddNamedComponent(component);

			return component;
		}

		#endregion

		/// <summary>
		/// Gets the next available ID from the list
		/// </summary>
		/// <returns></returns>
	    internal int GetNextId()
		{
			int checkValue = m_NextAvailableId;
			while (true)
			{
				if (!m_ElementsTypes.ContainsKey(checkValue))
				{
					m_NextAvailableId = checkValue + 1;
					return checkValue;
				}
				checkValue++;
			}
		}

		public Guid GetUuidForElementId(int id)
		{
			Guid uuid;
			return m_ElementUuids.TryGetValue(id, out uuid) ? uuid : Guid.Empty;
		}

		[CanBeNull]
		public string GetNameForElementId(int id)
		{
			string name;
			return m_ElementNames.TryGetValue(id, out name) ? name : null;
		}

		[CanBeNull]
		internal string GetXmlForElementId(int id)
		{
			string xml;
			return m_ElementsXml.TryGetValue(id, out xml) ? xml : null;
		}

		/// <summary>
		/// Get the type string for a control
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
	    internal Type GetTypeForId(int id)
	    {
		    Type typeString;
		    return m_ElementsTypes.TryGetValue(id, out typeString) ? typeString : null;
	    }

		/// <summary>
		/// Tries to get the specified Change Group
		/// If not found, returns null
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[CanBeNull]
		public IChangeGroup TryGetChangeGroup(int id)
		{
			IChangeGroup changeGroup;
			return m_ChangeGroups.TryGetValue(id, out changeGroup) ? changeGroup : null;
		}

		public IEnumerable<int> GetDefaultChangeGroups()
		{
			return m_DefaultChangeGroups.ToList(m_DefaultChangeGroups.Count);
		}

		public IEnumerable<KeyValuePair<int, Type>> GetElementsTypes()
		{
			return m_ElementsTypes.ToList(m_ElementsTypes.Count);
		}

		public IEnumerable<IChangeGroup> GetChangeGroups()
		{
			return m_ChangeGroups.Values.ToList(m_ChangeGroups.Count);
		}

		public IEnumerable<INamedControl> GetNamedControls()
		{
			return m_NamedControls.Values.ToList(m_NamedControls.Count);
		}

		public IEnumerable<INamedComponent> GetNamedComponents()
		{
			return m_NamedComponents.Values.ToList(m_NamedComponents.Count);
		}

		public IEnumerable<IQSysKrangControl> GetKrangControls()
		{
			return m_KrangControls.Values.ToList(m_KrangControls.Count);
		}

		#endregion

		#region Private Method

		/// <summary>
		/// Links a change group id to it's element id
		/// </summary>
		/// <param name="changeGroupId"></param>
		/// <param name="elementId"></param>
		private void BindChangeGroup(string changeGroupId, int elementId)
		{
			m_ChangeGroupBindings[changeGroupId] = elementId;
		}

		private void BindChangeGroup(IChangeGroup changeGroup)
		{
			BindChangeGroup(changeGroup.ChangeGroupId, changeGroup.Id);
		}

		/// <summary>
		/// Links a named conmponent qsys name to control id
		/// </summary>
		/// <param name="name"></param>
		/// <param name="id"></param>
		private void BindNamedComponent(string name, int id)
		{
			m_NamedComponentBindings[name] = id;
		}

		private void BindNamedComponent(INamedComponent namedComponent)
		{
			BindNamedComponent(namedComponent.ComponentName, namedComponent.Id);
		}

		/// <summary>
		/// Binds a Named control qsys name to control id
		/// </summary>
		/// <param name="name"></param>
		/// <param name="id"></param>
		private void BindNamedControl(string name, int id)
		{
			m_NamedControlBindings[name] = id;
		}

		private void BindNamedControl(INamedControl namedControl)
		{
			BindNamedControl(namedControl.ControlName, namedControl.Id);
		}

		#endregion
    }
}
