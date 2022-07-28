using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class MainZoneOnkyoAvrVolumeControl : AbstractOnkyoAvrVolumeControl
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="powerControl"></param>
        public MainZoneOnkyoAvrVolumeControl(IOnkyoAvrDevice parent, int id, IPowerDeviceControl powerControl) : base(parent, id, powerControl)
        { }

        /// <summary>
        /// Gets the maximum supported volume level.
        /// </summary>
        public override float VolumeLevelMax
        {
            get { return Parent.MaxVolume; }
        }
        protected override eOnkyoCommand VolumeCommand
        {
            get { return eOnkyoCommand.Volume; }
        }
        protected override eOnkyoCommand MuteCommand
        {
            get { return eOnkyoCommand.Mute; }
        }
    }
}