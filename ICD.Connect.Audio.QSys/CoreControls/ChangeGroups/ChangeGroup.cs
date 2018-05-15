using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.CoreControls.ChangeGroups
{
	public sealed class ChangeGroup : AbstractCoreControl, IChangeGroup
	{

		private const float DEFAULT_POLL_INTERVAL = (float).2;

	    private readonly SafeCriticalSection m_NamedControlCriticalSection;
	    private readonly List<INamedControl> m_NamedControls;

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
		    SendData(new ChangeGroupAddControlRpc(this, GetControls()).Serialize());
			SendData(new ChangeGroupAutoPollRpc(this).Serialize());
			SendData(new ChangeGroupPollRpc(this).Serialize());
	    }

	    public void DestroyChangeGroup()
	    {
		    SendData(new ChangeGroupDestroyRpc(this).Serialize());
	    }
    }
}
