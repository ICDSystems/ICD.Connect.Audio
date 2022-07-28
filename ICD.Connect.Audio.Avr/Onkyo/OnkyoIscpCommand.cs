using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public sealed class OnkyoIscpCommand : ISerialData
    {
        public const char DELIMITERS = '\x1A';
        
        private const char START_CHARACTER = '!';
        private const char UNIT_TYPE_CHARACTER = '1';
        private const char END_CHARACTER = '\x0D';
        public const string ISCP_HEADER = "ISCP";
        private const uint HEADER_SIZE = 0x10;
        private const uint VERSION_AND_RESERVED = 0x01000000;

        private const string QUERY_PARAMETER = "QSTN";
        private const string TOGGLE_PARAMETER = "TG";
        private const string UP_PARAMETER = "UP";
        private const string DOWN_PARAMETER = "DOWN";

        public const string ERROR_PARAMETER = "N/A";

        private readonly eOnkyoCommand m_Command;

        private readonly string m_Parameter;

        public bool AddEthernetHeader { get; set; }

        private OnkyoIscpCommand(eOnkyoCommand command, [NotNull] string parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException("parameter");

            m_Command = command;
            m_Parameter = parameter;
        }

        private string GetStringForCommand()
        {
            return m_Command.GetStringForCommand();
        }

        /// <summary>
        /// Serialize this instance to a string.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder command = new StringBuilder();
            
            // Add Ethernet Header if indicated
            if (AddEthernetHeader)
                command.Append(GetEthernetHeader());
                    
            command.Append(START_CHARACTER);
            command.Append(UNIT_TYPE_CHARACTER);
            command.Append(GetStringForCommand());
            command.Append(m_Parameter);
            command.Append(END_CHARACTER);

            return command.ToString();
        }

        private string GetEthernetHeader()
        {
            // Length is parameter length + 6
            uint commandLength = (uint)(6 + m_Parameter.Length);

            List<byte> output = new List<byte>();

            output.AddRange(StringUtils.ToBytes(ISCP_HEADER));
            output.AddRange(GetBytesBigEndian(HEADER_SIZE));
            output.AddRange(GetBytesBigEndian(commandLength));
            output.AddRange(GetBytesBigEndian(VERSION_AND_RESERVED));

            return StringUtils.ToString(output);
        }

        private static IEnumerable<byte> GetBytesBigEndian(uint intValue)
        {
            byte[] intBytes = BitConverter.GetBytes(intValue);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        private static string GetBoolParameter(bool state)
        {
            return state ? "01" : "00";
        }

        /// <summary>
        /// Converts the least significant byte of the parameter to a two
        /// character ascii string of it's hex representation
        /// Ex: 255 would be "FF", and 161 would be A1
        /// Other Bytes are ignored
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static string GetAsciiHexParameterString(int parameter)
        {
            // Take only the first byte;
            parameter &= 0xFF;
            return string.Format("{0:X2}", parameter);
        }

        #region Commands

        public static OnkyoIscpCommand GetSetCommand(eOnkyoCommand command, string parameter)
        {
            return new OnkyoIscpCommand(command, parameter);
        }

        public static OnkyoIscpCommand GetSetCommand(eOnkyoCommand command, bool state)
        {
            return new OnkyoIscpCommand(command, GetBoolParameter(state));
        }

        public static OnkyoIscpCommand GetSetCommand(eOnkyoCommand command, int parameter)
        {
            return new OnkyoIscpCommand(command, GetAsciiHexParameterString(parameter));
        }

        public static OnkyoIscpCommand GetQueryCommand(eOnkyoCommand command)
        {
            return new OnkyoIscpCommand(command, QUERY_PARAMETER);
        }

        public static OnkyoIscpCommand GetToggleCommand(eOnkyoCommand command)
        {
            return new OnkyoIscpCommand(command, TOGGLE_PARAMETER);
        }

        public static OnkyoIscpCommand GetUpCommand(eOnkyoCommand command)
        {
            return new OnkyoIscpCommand(command, UP_PARAMETER);
        }

        public static OnkyoIscpCommand GetDownCommand(eOnkyoCommand command)
        {
            return new OnkyoIscpCommand(command, DOWN_PARAMETER);
        }

        #endregion

        /// <summary>
        /// Delegate for parser callbacks
        /// </summary>
        public delegate void ResponseParserCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData);
    }
}