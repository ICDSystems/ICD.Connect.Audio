using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
    /// <summary>
    /// A boolean named control, adds ValueBool property and ToggleValue()
    /// </summary>
    public sealed class BooleanNamedControl : AbstractNamedControl
    {

        public bool ValueBool { get { return GetValueAsBool(ValueRaw); } }

        public BooleanNamedControl(QSysCoreDevice qSysCore, int id, string name, string controlName) : base(qSysCore, id, name, controlName)
        {
            
        }

	    /// <summary>
	    /// Sets the value from a bool
	    /// </summary>
	    /// <param name="value"></param>
	    public void SetValue(bool value)
	    {
		    base.SetValue(value);
	    }

	    public bool ToggleValue()
        {
            bool setValue = !ValueBool;
            SetValue(setValue);
            return setValue;
        }

	    /// <summary>
	    /// Gets the raw value as a boolean
	    /// </summary>
	    /// <param name="value"></param>
	    /// <returns></returns>
	    public static bool GetValueAsBool(float value)
	    {
		    return Math.Abs(value) > 0;
	    }

		#region Console

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
