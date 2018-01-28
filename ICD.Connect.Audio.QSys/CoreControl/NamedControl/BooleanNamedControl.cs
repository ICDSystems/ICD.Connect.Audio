using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

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

		/// <summary>
		/// Sets the value from a bool
		/// Changes to 0/1 since QSys doesn't accept "True" or "False"
		/// </summary>
		/// <param name="value"></param>
        public void SetValue(bool value) => base.SetValue(value ? 1 : 0);

        public bool ToggleValue()
        {
            bool setValue = !ValueBool;
            SetValue(setValue);
            return setValue;
        }

		#region Consoled

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
	    {
			base.BuildConsoleStatus(addRow);
		    addRow("Value Bool", ValueBool);
	    }

	    public override IEnumerable<IConsoleCommand> GetConsoleCommands()
	    {
		    foreach (IConsoleCommand command in GetBaseConsoleCommands())
			    yield return command;

		    yield return new ConsoleCommand("ToggleValue", "Toggles the current value", () => ToggleValue());
	    }

	    private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
	    {
		    return base.GetConsoleCommands();
	    }

	    #endregion
    }
}
