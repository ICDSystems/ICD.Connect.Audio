using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
    /// <summary>
    /// Represents a NamedControl
    /// </summary>
    public abstract class AbstractNamedControl : AbstractCoreControl, IConsoleNode
    {
	    public int Id { get; }

	    public string Name { get; }

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

		/// <summary>
		/// Sets the value of the control
		/// ToString() method is used to send the value
		/// </summary>
		/// <param name="value">Value to set the control to</param>
		public void SetValue(object value)
        {
            SendData(new ControlSetRpc(this, value).Serialize());
        }

        /// <summary>
        /// Polls the value of the control from the QSys Core
        /// </summary>
        public void PollValue()
        {
            SendData(new ControlGetRpc(this).Serialize());
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


        protected AbstractNamedControl(QSysCoreDevice qSysCore, int id, string name, string controlName) : base(qSysCore)
        {
	        Id = id;
	        Name = name;
            ControlName = controlName;
            //PollValue();
        }

	    protected virtual void Dispose(bool disposing)
	    {
		    if (disposing)
		    {
			    OnValueUpdated = null;
		    }
	    }

	    public void Dispose()
	    {
		    Dispose(true);
		    //GC.SuppressFinalize(this);
	    }

		#region Console

		public string ConsoleName { get { return Name; } }
	    public string ConsoleHelp { get { return String.Format("NamedControl: {0}" ,ControlName); } }

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
	    {
		    yield break;
	    }

	    public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
	    {
		    addRow("Id", Id);
		    addRow("Name", Name);
		    addRow("Control Name", ControlName);
		    addRow("Value Stirng", ValueString);
		    addRow("Value Raw", ValueRaw);
		    addRow("Value Position", ValuePosition);
	    }

	    public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
	    {
		    yield return new ConsoleCommand("Poll", "Pull The Current Value", () => PollValue());
			yield return new GenericConsoleCommand<string>("SetValue", "SetValue <Value>", p => SetValue(p));
	    }
		#endregion
	}
}
