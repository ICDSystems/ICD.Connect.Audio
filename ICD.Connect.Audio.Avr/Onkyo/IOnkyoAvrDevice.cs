using ICD.Common.Properties;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    /// <summary>
    /// Delegate for parser callbacks
    /// </summary>
    public delegate void ResponseParserCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData);
    
    public interface IOnkyoAvrDevice : IAvrDevice
    {
        /// <summary>
        /// The number of zones supported by the AVR
        /// </summary>
        int Zones { get; }
        
        /// <summary>
        /// Max volume supported by the AVR
        /// </summary>
        int MaxVolume { get; }

        /// <summary>
        /// Sends a command to the AVR
        /// </summary>
        /// <param name="command"></param>
        void SendCommand(OnkyoIscpCommand command);

        /// <summary>
        /// Registers the callback for feedback of the given command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        void RegisterCommandCallback(eOnkyoCommand command, ResponseParserCallback callback);
        
        /// <summary>
        /// Unregisters the callback for feedback of the given command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        void UnregisterCommandCallback(eOnkyoCommand command, ResponseParserCallback callback);
    }
}