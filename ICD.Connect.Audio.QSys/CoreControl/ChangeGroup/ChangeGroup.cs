using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.CoreControl.ChangeGroup
{
    public class ChangeGroup : AbstractCoreControl
    {

	    private readonly SafeCriticalSection m_NamedControlCriticalSection;
	    private readonly List<AbstractNamedControl> m_NamedControls;

		public int Id { get; private set; }

		public string Name { get; private set; }

		public string ChangeGroupId { get; private set; }

		public float? PollInterval { get; private set; }

	    public ChangeGroup(QSysCoreDevice qSysCore, int id, string name, string changeGroupId) : base(qSysCore)
	    {
			Id = id;
		    Name = name;
		    ChangeGroupId = changeGroupId;
		    PollInterval = null;

			m_NamedControls = new List<AbstractNamedControl>();
			m_NamedControlCriticalSection = new SafeCriticalSection();
	    }

		public ChangeGroup(QSysCoreDevice qSysCore, int id, string name, string changeGroupId, float? pollInterval) : base(qSysCore)
	    {
		    Id = id;
		    Name = name;
		    ChangeGroupId = changeGroupId;
		    PollInterval = pollInterval;

		    m_NamedControls = new List<AbstractNamedControl>();
		    m_NamedControlCriticalSection = new SafeCriticalSection();
		}

	    public void AddNamedControl(AbstractNamedControl control)
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

	    public void AddNamedControl(IEnumerable<AbstractNamedControl> controls)
	    {
		    bool firstItem = false;
		    m_NamedControlCriticalSection.Enter();
		    try
		    {
			    if (m_NamedControls.Count == 0)
				    firstItem = true;
			    m_NamedControls.AddRange(controls);
		    }
		    finally
		    {
			    m_NamedControlCriticalSection.Leave();
		    }

		    SendData(new ChangeGroupAddControlRpc(this, controls).Serialize());
		    if (firstItem)
			    SetAutoPoll();

	    }

		public IEnumerable<AbstractNamedControl> GetControls()
	    {
			List<AbstractNamedControl> controls;
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
	    }

	    public void DestroyChangeGroup()
	    {
		    SendData(new ChangeGroupDestroyRpc(this).Serialize());
	    }
    }
}
