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
		/// <summary>
		/// Change Groups
		/// </summary>
		private Dictionary<string, IChangeGroup> m_ChangeGroups;
		private Dictionary<int, IChangeGroup> m_ChangeGroupsById;
		private readonly SafeCriticalSection m_ChangeGroupsCriticalSection;

		/// <summary>
		/// Named Controls
		/// </summary>
		private Dictionary<string, INamedControl> m_NamedControls;
		private Dictionary<int, INamedControl> m_NamedControlsById;
		private readonly SafeCriticalSection m_NamedControlsCriticalSection;

		/// <summary>
		/// Named Components
		/// </summary>
		private Dictionary<string, INamedComponent> m_NamedComponents;
		private Dictionary<int, INamedComponent> m_NamedComponentsById;
		private readonly SafeCriticalSection m_NamedComponentsCriticalSection;

		private readonly IcdHashSet<IDeviceControl> m_LoadedControls;
		private readonly QSysCoreDevice m_Parent;

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreComponentsCollection(QSysCoreDevice parent)
		{
			m_Parent = parent;

			m_ChangeGroupsCriticalSection = new SafeCriticalSection();
			m_ChangeGroups = new Dictionary<string, IChangeGroup>();
			m_ChangeGroupsById = new Dictionary<int, IChangeGroup>();

			m_NamedControlsCriticalSection = new SafeCriticalSection();
			m_NamedControls = new Dictionary<string, INamedControl>();
			m_NamedControlsById = new Dictionary<int, INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<string, INamedComponent>();
			m_NamedComponentsById = new Dictionary<int, INamedComponent>();

			m_LoadedControls = new IcdHashSet<IDeviceControl>();
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

			CoreElementsLoadContext loadContext = CoreElementsXmlUtils.GetControlsFromXml(xml, m_Parent);

			// Add to correct collections
			AddChangeGroup(loadContext.GetChangeGroups());
			AddNamedControl(loadContext.GetNamedControls());
			AddNamedComponent(loadContext.GetNamedComponents());
			AddKrangControl(loadContext.GetKrangControls());
		}

		public void Initialize()
		{
			m_ChangeGroupsCriticalSection.Execute(() => m_ChangeGroups.ForEach(k => k.Value.Initialize()));
		}

		public void AddChangeGroup(IEnumerable<IChangeGroup> changeGroups)
		{
			changeGroups.ForEach(AddChangeGroup);
		}

		public void AddChangeGroup(IChangeGroup changeGroup)
		{
			m_ChangeGroupsCriticalSection.Enter();

			try
			{
				m_ChangeGroups.Add(changeGroup.ChangeGroupId, changeGroup);
				m_ChangeGroupsById.Add(changeGroup.Id, changeGroup);
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}
		}

		[CanBeNull]
		public IChangeGroup GetChangeGroup(string changeGroupId)
		{
			return m_ChangeGroupsCriticalSection.Execute(() => m_ChangeGroups.GetDefault(changeGroupId));
		}

		public void AddNamedControl(IEnumerable<INamedControl> namedControls)
		{
			namedControls.ForEach(AddNamedControl);
		}

		public void AddNamedControl(INamedControl namedControl)
		{
			m_NamedControlsCriticalSection.Enter();

			try
			{
				m_NamedControls.Add(namedControl.ControlName, namedControl);
				m_NamedControlsById.Add(namedControl.Id, namedControl);
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}
		}

		public void AddNamedComponent(IEnumerable<INamedComponent> namedComponents)
		{
			namedComponents.ForEach(AddNamedComponent);
		}

		public void AddNamedComponent(INamedComponent namedComponent)
		{
			m_NamedComponentsCriticalSection.Enter();

			try
			{
				m_NamedComponents.Add(namedComponent.ComponentName, namedComponent);
				m_NamedComponentsById.Add(namedComponent.Id, namedComponent);
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}
		}

		public void AddKrangControl(IEnumerable<IQSysKrangControl> krangControls)
		{
			krangControls.ForEach(AddKrangControl);
		}

		public void AddKrangControl(IQSysKrangControl krangControl)
		{
			m_Parent.Controls.Add(krangControl);
		}

		public IEnumerable<IChangeGroup> GetChangeGroups()
		{
			return m_ChangeGroupsCriticalSection.Execute(() => m_ChangeGroups.Values.ToArray(m_ChangeGroups.Count));
		}

		public IEnumerable<INamedControl> GetNamedControls()
		{
			return m_NamedControlsCriticalSection.Execute(() => m_NamedControls.Values.ToArray(m_NamedControls.Count));
		}

		public IEnumerable<INamedComponent> GetNamedComponents()
		{
			return m_NamedComponentsCriticalSection.Execute(() => m_NamedComponents.Values.ToArray(m_NamedComponents.Count));
		}

		public void ClearLoadedControls()
		{
			// Clear Change Groups
			m_ChangeGroupsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, IChangeGroup> kvp in m_ChangeGroups)
				{
					kvp.Value.DestroyChangeGroup();
					kvp.Value.Dispose();
				}
				m_ChangeGroups = new Dictionary<string, IChangeGroup>();
				m_ChangeGroupsById = new Dictionary<int, IChangeGroup>();
			}
			finally
			{
				m_ChangeGroupsCriticalSection.Leave();
			}

			// Clear Named Controls
			m_NamedControlsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedControl> kvp in m_NamedControls)
					kvp.Value.Dispose();
				m_NamedControls = new Dictionary<string, INamedControl>();
				m_NamedControlsById = new Dictionary<int, INamedControl>();
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}

			// Clear Named Components
			m_NamedComponentsCriticalSection.Enter();
			try
			{
				foreach (KeyValuePair<string, INamedComponent> kvp in m_NamedComponents)
					kvp.Value.Dispose();
				m_NamedComponents = new Dictionary<string, INamedComponent>();
				m_NamedComponentsById = new Dictionary<int, INamedComponent>();
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			// Clear Controls Collection
			foreach (IDeviceControl control in m_LoadedControls)
			{
				control.Dispose();
				m_Parent.Controls.Remove(control.Id);
			}

			m_LoadedControls.Clear();
		}

		public bool TryGetNamedComponent(string nameToken, out INamedComponent component)
		{
			m_NamedComponentsCriticalSection.Enter();

			try
			{
				return m_NamedComponents.TryGetValue(nameToken, out component);
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}
		}

		public bool TryGetNamedControl(string nameToken, out INamedControl control)
		{
			m_NamedControlsCriticalSection.Enter();

			try
			{
				return m_NamedControls.TryGetValue(nameToken, out control);
			}
			finally
			{
				m_NamedControlsCriticalSection.Leave();
			}
		}
	}
}
