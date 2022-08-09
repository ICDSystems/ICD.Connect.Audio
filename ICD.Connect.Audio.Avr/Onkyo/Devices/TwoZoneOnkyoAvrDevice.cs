using System;
using ICD.Connect.Audio.Avr.Onkyo.Controls;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Avr.Onkyo.Devices
{
    public sealed class TwoZoneOnkyoAvrDevice : AbstractOnkyoAvrDevice<TwoZoneOnkyoAvrDeviceSettings>
    {
        /// <summary>
        /// Power control for zone 2 
        /// </summary>
        private Zone2OnkyoAvrPowerControl m_Zone2PowerControl;
        
        /// <summary>
        /// The number of zones supported by the AVR
        /// </summary>
        public override int Zones
        {
            get { return 2; }
        }

        /// <summary>
        /// Override to add controls to the device.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="addControl"></param>
        protected override void AddControls(TwoZoneOnkyoAvrDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
        {
            base.AddControls(settings, factory, addControl);

            m_Zone2PowerControl = new Zone2OnkyoAvrPowerControl(this, 20);
            addControl(m_Zone2PowerControl);
            addControl(new Zone2OnkyoAvrVolumeControl(this, 21, m_Zone2PowerControl));
        }

        protected override OnkyoAvrRouteSwitcherControl GetSwitcherControl(IPowerDeviceControl zone1Control)
        {
            return new OnkyoAvrRouteSwitcherControl(this, 0, zone1Control, m_Zone2PowerControl);
        }
    }
}