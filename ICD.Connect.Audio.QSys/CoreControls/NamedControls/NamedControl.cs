namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
    /// <summary>
    /// Represents a generic named control
    /// </summary>
    public sealed class NamedControl : AbstractNamedControl
    {
        public NamedControl(QSysCoreDevice qSysCore, int id, string name, string controlName) : base(qSysCore, id, name, controlName)
        {
            
        }
    }
}
