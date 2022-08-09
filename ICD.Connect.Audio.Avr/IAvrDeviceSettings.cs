using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Avr
{
    public interface IAvrDeviceSettings : IDeviceSettings
    {
        /// <summary>
        /// When true, routing to an output will power the associated zone on, and unrouting will power it off.
        /// </summary>
        bool SetZonePowerWithRouting { get; set; }
    }
}