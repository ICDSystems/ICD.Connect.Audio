namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
    /// <summary>
    /// A boolean named control, adds ValueBool property and ToggleValue()
    /// </summary>
    sealed class BooleanNamedControl : AbstractNamedControl
    {

        public bool ValueBool { get { return ValueRaw != 0; } }

        public BooleanNamedControl(QSysCoreDevice qSysCore, int id, string name, string controlName) : base(qSysCore, id, name, controlName)
        {
            
        }

        public void SetValue(bool value) => base.SetValue(value);

        public bool ToggleValue()
        {
            bool setValue = !ValueBool;
            SetValue(setValue);
            return setValue;
        }
    }
}
