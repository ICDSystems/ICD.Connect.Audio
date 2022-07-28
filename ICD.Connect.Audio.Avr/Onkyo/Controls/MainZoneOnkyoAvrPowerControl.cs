namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class MainZoneOnkyoAvrPowerControl : AbstractOnkyoAvrPowerControl
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public MainZoneOnkyoAvrPowerControl(IOnkyoAvrDevice parent, int id) : base(parent, id)
        { }

        protected override eOnkyoCommand PowerCommand
        {
            get { return eOnkyoCommand.Power; }
        }
    }
}