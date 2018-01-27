using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
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
        public float ValueValue { get; private set; }

        /// <summary>
        /// Position representation of the control value
        /// This is a number between 0 and 1
        /// Representing the relative position of the control
        /// </summary>
        public float ValuePosition { get; private set; }

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
        /// <param name="valueValue">Raw value of the control</param>
        /// <param name="valuePosition">Position value of the control</param>
        public void SetFeedback(string valueString, float valueValue, float valuePosition)
        {
            ValueString = valueString;
            ValueValue = valueValue;
            ValuePosition = valuePosition;
        }

        #endregion


        protected AbstractNamedControl(QSysCoreDevice qSysCore, string controlName) : base(qSysCore)
        {
            ControlName = controlName;
            PollValue();
        }


    }
}
