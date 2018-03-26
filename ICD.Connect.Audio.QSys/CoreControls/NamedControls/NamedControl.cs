namespace ICD.Connect.Audio.QSys.CoreControls.NamedControls
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
