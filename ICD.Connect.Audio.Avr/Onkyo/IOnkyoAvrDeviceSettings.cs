using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public interface IOnkyoAvrDeviceSettings : IAvrDeviceSettings, INetworkProperties, IComSpecProperties
    {
        /// <summary>
        /// The port id.
        /// </summary>
        [OriginatorIdSettingsProperty(typeof(ISerialPort))]
        int? Port { get; set; }

        int MaxVolume { get; set; }
        
        eCommunicationsType CommunicationsType { get; set; }
    }
}