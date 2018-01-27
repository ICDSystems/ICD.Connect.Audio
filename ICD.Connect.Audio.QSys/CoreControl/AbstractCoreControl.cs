namespace ICD.Connect.Audio.QSys.CoreControl
{
    /// <summary>
    /// Represents a control on the QSys Core, either a NamedControl or a NamedComponent
    /// </summary>
    public abstract class AbstractCoreControl
    {

        private readonly QSysCoreDevice m_Core;

        protected void SendData(string data)
        {
            m_Core.SendData(data);
        }

        protected AbstractCoreControl(QSysCoreDevice qSysCore)
        {
            m_Core = qSysCore;
        }
    }
}
