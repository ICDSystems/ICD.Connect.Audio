using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.CoreControls
{
    /// <summary>
    /// Represents a control on the QSys Core, either a NamedControl or a NamedComponent
    /// </summary>
    public abstract class AbstractCoreControl : AbstractDeviceControl<QSysCoreDevice>
    {

	    private readonly string m_Name;

	    public override string Name
	    {
		    get { return m_Name; }
	    }

	    protected void SendData(string data)
        {
			Parent.SendData(data);
        }

        protected AbstractCoreControl(QSysCoreDevice qSysCore, string name, int id): base(qSysCore, id)
        {
			m_Name = name;
        }
    }
}
