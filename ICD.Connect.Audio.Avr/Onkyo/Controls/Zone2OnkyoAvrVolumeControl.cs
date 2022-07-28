using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class Zone2OnkyoAvrVolumeControl : AbstractOnkyoAvrVolumeControl
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="powerControl"></param>
        public Zone2OnkyoAvrVolumeControl(IOnkyoAvrDevice parent, int id, IPowerDeviceControl powerControl) : base(parent, id, powerControl)
        { }

        /// <summary>
        /// Gets the maximum supported volume level.
        /// </summary>
        public override float VolumeLevelMax 
        {
            get { return OTHER_ZONE_DEFAULT_VOLUME; }
        }

        protected override eOnkyoCommand VolumeCommand
        {
            get { return eOnkyoCommand.Zone2Volume; }
        }

        protected override eOnkyoCommand MuteCommand
        {
            get { return eOnkyoCommand.Zone2Mute; }
        }
    }
}