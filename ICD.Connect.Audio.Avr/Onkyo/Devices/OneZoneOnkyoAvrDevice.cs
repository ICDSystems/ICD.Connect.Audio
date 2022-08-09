using ICD.Connect.Audio.Avr.Onkyo.Controls;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Audio.Avr.Onkyo.Devices
{
    public sealed class OneZoneOnkyoAvrDevice : AbstractOnkyoAvrDevice<OneZoneOnkyoAvrDeviceSettings>
    {
        /// <summary>
        /// The number of zones supported by the AVR
        /// </summary>
        public override int Zones
        {
            get { return 1; }
        }

        protected override OnkyoAvrRouteSwitcherControl GetSwitcherControl(IPowerDeviceControl zone1Control)
        {
            return new OnkyoAvrRouteSwitcherControl(this, 0, zone1Control);
        }
    }
}