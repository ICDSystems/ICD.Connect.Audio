namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class Zone2OnkyoAvrPowerControl : AbstractOnkyoAvrPowerControl
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public Zone2OnkyoAvrPowerControl(IOnkyoAvrDevice parent, int id) : base(parent, id)
        { }

        protected override eOnkyoCommand PowerCommand
        {
            get { return eOnkyoCommand.Zone2Power; }
        }
    }
}