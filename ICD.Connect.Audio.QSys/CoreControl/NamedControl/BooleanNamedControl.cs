using ICD.Connect.Audio.QSys.CoreControl.NamedControl;

namespace ICD.Connect.Audio.QSys.NamedControls
{
    /// <summary>
    /// A boolean named control, adds ValueBool property and ToggleValue()
    /// </summary>
    sealed class BooleanNamedControl : AbstractNamedControl
    {

        public bool ValueBool { get { return ValueValue != 0; } }

        public BooleanNamedControl(QSysCoreDevice qSysCore, string controlName) : base(qSysCore, controlName)
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
