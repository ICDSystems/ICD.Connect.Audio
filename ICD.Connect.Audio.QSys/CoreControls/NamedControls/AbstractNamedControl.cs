using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Rpc;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedControls
{
    /// <summary>
    /// Represents a NamedControl
    /// </summary>
    public abstract class AbstractNamedControl : AbstractCoreControl, INamedControl
	{
	    /// <summary>
        /// Name of the control on the QSys Core
        /// </summary>
        public string ControlName { get; private set; }

        /// <summary>
        /// String representation of the control value
        /// </summary>
        public string ValueString { get; private set; }

        /// <summary>
        /// Float representation of the control value
        /// </summary>
        public float ValueRaw { get; private set; }

        /// <summary>
        /// Position representation of the control value
        /// This is a number between 0 and 1
        /// Representing the relative position of the control
        /// </summary>
        public float ValuePosition { get; private set; }

		#region events

	    public event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		#endregion

		#region methods

	    public void TriggerControl()
	    {
		    SetPosition(1);
	    }

	    /// <summary>
		/// Sets the value of the control
		/// ToString() method is used to send the value
		/// </summary>
		/// <param name="value">Value to set the control to</param>
		public void SetValue(object value)
        {
            SendData(new ControlSetValueRpc(this, value).Serialize());
        }

	    public void SetPosition(float position)
	    {
			SendData(new ControlSetPositionRpc(this, position).Serialize());
		}

	    /// <summary>
		/// Polls the value of the control from the QSys Core
		/// </summary>
		public void PollValue()
        {
            SendData(new ControlGetRpc(this).Serialize());
        }

	    public void ParseFeedback(JToken feedback)
	    {
		    if (feedback == null)
			    throw new ArgumentNullException("feedback");

		    string valueString = (string)feedback.SelectToken("String");
		    float valueValue = (float)feedback.SelectToken("Value");
		    float valuePostion = (float)feedback.SelectToken("Position");

		    SetFeedback(valueString, valueValue, valuePostion);
		}

		#endregion

		#region internal methods

		/// <summary>
		/// Called by the Core to update feedback for the control
		/// </summary>
		/// <param name="valueString">String value of the control</param>
		/// <param name="valueRaw">Raw value of the control</param>
		/// <param name="valuePosition">Position value of the control</param>
		internal void SetFeedback(string valueString, float valueRaw, float valuePosition)
        {
            ValueString = valueString;
            ValueRaw = valueRaw;
            ValuePosition = valuePosition;

	        OnValueUpdated.Raise(this, new ControlValueUpdateEventArgs(ControlName, ValueString, ValueRaw, ValuePosition));
		}

        #endregion


        protected AbstractNamedControl(QSysCoreDevice qSysCore, int id, string name, string controlName) : base(qSysCore, name, id)
        {
            ControlName = controlName;
            //PollValue();
        }

	    protected override void DisposeFinal(bool disposing)
	    {
		    OnValueUpdated = null;
		    base.DisposeFinal(disposing);
	    }

	    #region Console

	    public override string ConsoleHelp { get { return string.Format("NamedControl: {0}" ,ControlName); } }

	    public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
	    {
			base.BuildConsoleStatus(addRow);
		    addRow("Control Name", ControlName);
		    addRow("Value Stirng", ValueString);
		    addRow("Value Raw", ValueRaw);
		    addRow("Value Position", ValuePosition);
	    }

	    public override IEnumerable<IConsoleCommand> GetConsoleCommands()
	    {
		    foreach (IConsoleCommand command in GetBaseConsoleCommands())
			    yield return command;
		    yield return new ConsoleCommand("Poll", "Pull The Current Value", () => PollValue());
		    yield return new ConsoleCommand("Trigger", "Triggers the control", () => TriggerControl());
			yield return new GenericConsoleCommand<string>("SetPosition", "SetPosition <Position>" ,p => SetPosition(float.Parse(p)));
			yield return new GenericConsoleCommand<string>("SetValue", "SetValue <Value>", p => SetValue(p));
	    }

	    private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
	    {
		    return base.GetConsoleCommands();
	    }

	    #endregion
	}
}
