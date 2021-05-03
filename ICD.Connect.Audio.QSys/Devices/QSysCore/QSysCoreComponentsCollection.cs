using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore
{
	public sealed class QSysCoreComponentsCollection : IDisposable
	{
		private readonly Dictionary<string, IChangeGroup> m_ChangeGroups;
		private readonly Dictionary<string, INamedControl> m_NamedControls;
		private readonly Dictionary<string, INamedComponent> m_NamedComponents;

		private readonly SafeCriticalSection m_CollectionSection;
		private readonly IcdHashSet<IDeviceControl> m_LoadedControls;
		private readonly QSysCoreDevice m_Parent;

		private CoreElementsLoadContext m_LoadContext;

		internal CoreElementsLoadContext LoadContext { get { return m_LoadContext; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreComponentsCollection(QSysCoreDevice parent)
		{
			m_Parent = parent;

			m_ChangeGroups = new Dictionary<string, IChangeGroup>();
			m_NamedControls = new Dictionary<string, INamedControl>();
			m_NamedComponents = new Dictionary<string, INamedComponent>();

			m_CollectionSection = new SafeCriticalSection();
			m_LoadedControls = new IcdHashSet<IDeviceControl>();

			m_LoadContext = new CoreElementsLoadContext(m_Parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			ClearLoadedControls();
		}

		public void ParseXml(string xml)
		{
			ClearLoadedControls();

			m_LoadContext = CoreElementsXmlUtils.GetControlsFromXml(xml, m_Parent);

			// Add to correct collections
			AddChangeGroup(m_LoadContext.GetChangeGroups());
			AddNamedControl(m_LoadContext.GetNamedControls());
			AddNamedComponent(m_LoadContext.GetNamedComponents());
			AddKrangControl(m_LoadContext.GetKrangControls());
		}

		public void Initialize()
		{
			m_CollectionSection.Execute(() => m_ChangeGroups.ForEach(k => k.Value.Initialize()));
		}

		public void AddChangeGroup(IEnumerable<IChangeGroup> changeGroups)
		{
			changeGroups.ForEach(AddChangeGroup);
		}

		public void AddChangeGroup(IChangeGroup changeGroup)
		{
			m_CollectionSection.Execute(() => m_ChangeGroups.Add(changeGroup.ChangeGroupId, changeGroup));
		}

		[CanBeNull]
		public IChangeGroup GetChangeGroup(string changeGroupId)
		{
			return m_CollectionSection.Execute(() => m_ChangeGroups.GetDefault(changeGroupId));
		}

		public void AddNamedControl(IEnumerable<INamedControl> namedControls)
		{
			namedControls.ForEach(AddNamedControl);
		}

		public void AddNamedControl(INamedControl namedControl)
		{
			m_CollectionSection.Execute(() => m_NamedControls.Add(namedControl.ControlName, namedControl));
		}

		public void AddNamedComponent(IEnumerable<INamedComponent> namedComponents)
		{
			namedComponents.ForEach(AddNamedComponent);
		}

		public void AddNamedComponent(INamedComponent namedComponent)
		{
			m_CollectionSection.Execute(() => m_NamedComponents.Add(namedComponent.ComponentName, namedComponent));
		}

		public void AddKrangControl(IEnumerable<IQSysKrangControl> krangControls)
		{
			krangControls.ForEach(AddKrangControl);
		}

		public void AddKrangControl(IQSysKrangControl krangControl)
		{
			m_CollectionSection.Enter();

			try
			{
				m_Parent.Controls.Add(krangControl);
				m_LoadedControls.Add(krangControl);
			}
			finally
			{
				m_CollectionSection.Leave();
			}
		}

		public IEnumerable<IChangeGroup> GetChangeGroups()
		{
			return m_CollectionSection.Execute(() => m_ChangeGroups.Values.ToArray(m_ChangeGroups.Count));
		}

		public IEnumerable<INamedControl> GetNamedControls()
		{
			return m_CollectionSection.Execute(() => m_NamedControls.Values.ToArray(m_NamedControls.Count));
		}

		public IEnumerable<INamedComponent> GetNamedComponents()
		{
			return m_CollectionSection.Execute(() => m_NamedComponents.Values.ToArray(m_NamedComponents.Count));
		}

		public void ClearLoadedControls()
		{
			// Clear Change Groups
			m_CollectionSection.Enter();
			try
			{
				foreach (KeyValuePair<string, IChangeGroup> kvp in m_ChangeGroups)
				{
					kvp.Value.DestroyChangeGroup();
					kvp.Value.Dispose();
				}
				m_ChangeGroups.Clear();
			}
			finally
			{
				m_CollectionSection.Leave();
			}

			// Clear Named Controls
			m_CollectionSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedControl> kvp in m_NamedControls)
					kvp.Value.Dispose();
				m_NamedControls.Clear();
			}
			finally
			{
				m_CollectionSection.Leave();
			}

			// Clear Named Components
			m_CollectionSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedComponent> kvp in m_NamedComponents)
					kvp.Value.Dispose();
				m_NamedComponents.Clear();
			}
			finally
			{
				m_CollectionSection.Leave();
			}

			// Clear Controls Collection
			foreach (IDeviceControl control in m_LoadedControls)
			{
				m_Parent.Controls.Remove(control.Id);
				control.Dispose();
			}

			m_LoadedControls.Clear();
		}

		public bool TryGetNamedComponent(string nameToken, out INamedComponent component)
		{
			m_CollectionSection.Enter();

			try
			{
				return m_NamedComponents.TryGetValue(nameToken, out component);
			}
			finally
			{
				m_CollectionSection.Leave();
			}
		}

		public bool TryGetNamedControl(string nameToken, out INamedControl control)
		{
			m_CollectionSection.Enter();

			try
			{
				return m_NamedControls.TryGetValue(nameToken, out control);
			}
			finally
			{
				m_CollectionSection.Leave();
			}
		}

		public T LazyLoadNamedComponent<T>(string componentName)
			where T : INamedComponent
		{
			m_CollectionSection.Enter();

			try
			{
				INamedComponent component;
				if (!m_NamedComponents.TryGetValue(componentName, out component))
				{
					component = m_LoadContext.LazyLoadNamedComponent<T>(componentName);
					AddNamedComponent(component);
				}

				return (T)component;
			}
			finally
			{
				m_CollectionSection.Leave();
			}
		}
	}
}
