using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.CoreControls.ChangeGroups
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
		public ChangeGroup(int id, string friendlyName, CoreElementsLoadContext context, string xml):base(context.QSysCore, friendlyName, id)
		{
			m_NamedControlCriticalSection = new SafeCriticalSection();
			m_NamedControls = new List<INamedControl>();

			m_NamedComponentsCriticalSection = new SafeCriticalSection();
			m_NamedComponents = new Dictionary<INamedComponent, List<INamedComponentControl>>();

			ChangeGroupId = XmlUtils.GetAttributeAsString(xml, "changeGroupId");
			try
			{
				PollInterval = float.Parse(XmlUtils.GetAttributeAsString(xml, "pollInterval"));
			}
			catch (FormatException e)
			{
			}
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

			SendData(new ChangeGroupAddControlRpc(this, control).Serialize());
			if (firstItem)
				SetAutoPoll();
	    }

		public void AddNamedControl(IEnumerable<INamedControl> controls)
	    {
		    bool firstItem = false;
		    m_NamedControlCriticalSection.Enter();
		    IEnumerable<INamedControl> namedControls = controls as IList<INamedControl> ?? controls.ToList();
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

		    SendData(new ChangeGroupAddControlRpc(this, namedControls).Serialize());
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
				if (!m_NamedComponents.ContainsKey(component))
					m_NamedComponents[component] = new List<INamedComponentControl>();

				// Add controls to component
				m_NamedComponents[component].AddRange(controlsList);

			}
			finally
			{
				m_NamedComponentsCriticalSection.Leave();
			}

			// Send subscribe to Core
			SendData(new ChangeGroupAddComponentControlRpc(this, component, controlsList).Serialize());
		}

		public IEnumerable<INamedControl> GetControls()
	    {
			List<INamedControl> controls;
		    m_NamedControlCriticalSection.Enter();
		    try
		    {
			    controls = m_NamedControls.ToList();
		    }
		    finally
		    {
			    m_NamedControlCriticalSection.Leave();
		    }

		    return controls;
	    }

	    public void SetAutoPoll()
	    {
			if (PollInterval != null)
				SendData(new ChangeGroupAutoPollRpc(this).Serialize());
	    }

	    public void SetAutoPoll(float? pollInterval)
	    {
		    PollInterval = pollInterval;
			SetAutoPoll();
	    }

	    public void Initialize()
	    {
			// Send Named Controls
		    SendData(new ChangeGroupAddControlRpc(this, GetControls()).Serialize());

			// Send Named Components
			m_NamedComponentsCriticalSection.Enter();
		    try
		    {
			    foreach (KeyValuePair<INamedComponent, List<INamedComponentControl>> kvp in m_NamedComponents)
					SendData(new ChangeGroupAddComponentControlRpc(this, kvp.Key, kvp.Value).Serialize());

		    }
		    finally
		    {
			    m_NamedComponentsCriticalSection.Leave();
		    }

			// Setup Auto-Polling
		    SendData(new ChangeGroupAutoPollRpc(this).Serialize());
			SendData(new ChangeGroupPollRpc(this).Serialize());
	    }

	    public void DestroyChangeGroup()
	    {
		    SendData(new ChangeGroupDestroyRpc(this).Serialize());
	    }
    }
}
