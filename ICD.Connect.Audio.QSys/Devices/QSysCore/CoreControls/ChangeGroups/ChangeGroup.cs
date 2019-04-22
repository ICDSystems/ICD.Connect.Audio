using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups
{
	public sealed class ChangeGroup : AbstractCoreControl, IChangeGroup
	{
		private const float DEFAULT_POLL_INTERVAL = 0.5f;

		private readonly SafeCriticalSection m_NamedControlCriticalSection;
		private readonly List<INamedControl> m_NamedControls;

		private readonly SafeCriticalSection m_NamedComponentsCriticalSection;
		private readonly Dictionary<INamedComponent, List<INamedComponentControl>> m_NamedComponents;

		public string ChangeGroupId { get; private set; }

		public float? PollInterval { get; private set; }

		/// <summary>
		/// Constructor for Explicitly Defined Change Groups
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public ChangeGroup(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, friendlyName, id)
		{
			m_NamedControlCriticalSection = new SafeCriticalSection();
			m_NamedControls = new List<INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<INamedComponent, List<INamedComponentControl>>();

			ChangeGroupId = XmlUtils.GetAttributeAsString(xml, "changeGroupId");

			float pollInterval;
			if (StringUtils.TryParse(XmlUtils.GetAttributeAsString(xml, "pollInterval"), out pollInterval))
				PollInterval = pollInterval;
		}

		/// <summary>
		/// Constructor for Implicitly Defined Change Groups
		/// </summary>
		/// <param name="id"></param>
		/// <param name="context"></param>
		/// <param name="changeGroupId"></param>
		[UsedImplicitly]
		public ChangeGroup(int id, CoreElementsLoadContext context, string changeGroupId)
			: base(context.QSysCore, String.Format("Implicit Change  Group {0}", changeGroupId), id)
		{
			m_NamedControlCriticalSection = new SafeCriticalSection();
			m_NamedControls = new List<INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<INamedComponent, List<INamedComponentControl>>();

			ChangeGroupId = changeGroupId;
			PollInterval = DEFAULT_POLL_INTERVAL;
		}

		public void AddNamedControl(INamedControl control)
		{
			bool firstItem = false;

			m_NamedControlCriticalSection.Enter();

			try
			{
				if (m_NamedControls.Count == 0)
					firstItem = true;

				m_NamedControls.Add(control);
			}
			finally
			{
				m_NamedControlCriticalSection.Leave();
			}

			SendData(new ChangeGroupAddControlRpc(this, control));
			if (firstItem)
				SetAutoPoll();
		}

		public void AddNamedControl(IEnumerable<INamedControl> controls)
		{
			IEnumerable<INamedControl> namedControls = controls as IList<INamedControl> ?? controls.ToArray();
			bool firstItem = false;

			m_NamedControlCriticalSection.Enter();
			
			try
			{
				if (m_NamedControls.Count == 0)
					firstItem = true;

				m_NamedControls.AddRange(namedControls);
			}
			finally
			{
				m_NamedControlCriticalSection.Leave();
			}

			SendData(new ChangeGroupAddControlRpc(this, namedControls));
			if (firstItem)
				SetAutoPoll();
		}

		public void AddNamedComponent(INamedComponent component)
		{
			AddNamedComponent(component, component.GetControls());
		}

		public void AddNamedComponent(INamedComponent component, IEnumerable<INamedComponentControl> controls)
		{
			IList<INamedComponentControl> controlsList = controls as IList<INamedComponentControl> ?? controls.ToArray();

			m_NamedComponentsCriticalSection.Enter();

			try
			{
				// If component isn't in dict, add it
				List<INamedComponentControl> cache;
				if (!m_NamedComponents.TryGetValue(component, out cache))
				{
					cache = new List<INamedComponentControl>();
					m_NamedComponents.Add(component, cache);
				}

				// Add controls to component
				cache.AddRange(controlsList);
			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			// Send subscribe to Core
			SendData(new ChangeGroupAddComponentControlRpc(this, component, controlsList));
		}

		public IEnumerable<INamedControl> GetControls()
		{
			return m_NamedControlCriticalSection.Execute(() => m_NamedControls.ToArray());
		}

		public void SetAutoPoll()
		{
			if (PollInterval != null)
				SendData(new ChangeGroupAutoPollRpc(this));
		}

		public void SetAutoPoll(float? pollInterval)
		{
			PollInterval = pollInterval;
			SetAutoPoll();
		}

		public void Initialize()
		{
			// Send Named Controls
			SendData(new ChangeGroupAddControlRpc(this, GetControls()));

			// Send Named Components
			foreach (KeyValuePair<INamedComponent, List<INamedComponentControl>> kvp in
				m_NamedComponentsCriticalSection.Execute(() => m_NamedComponents.ToArray()))
				SendData(new ChangeGroupAddComponentControlRpc(this, kvp.Key, kvp.Value));

			// Setup Auto-Polling
			SendData(new ChangeGroupAutoPollRpc(this));
			SendData(new ChangeGroupPollRpc(this));
		}

		public void DestroyChangeGroup()
		{
			SendData(new ChangeGroupDestroyRpc(this));
		}
	}
}
