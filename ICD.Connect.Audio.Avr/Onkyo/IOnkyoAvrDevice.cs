using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public interface IOnkyoAvrDevice : IDevice
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
        void RegisterCommandCallback(eOnkyoCommand command, OnkyoIscpCommand.ResponseParserCallback callback);
        
        /// <summary>
        /// Unregisters the callback for feedback of the given command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        void UnregisterCommandCallback(eOnkyoCommand command, OnkyoIscpCommand.ResponseParserCallback callback);
    }
}