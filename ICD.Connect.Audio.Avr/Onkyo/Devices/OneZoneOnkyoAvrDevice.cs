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
    }
}