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

        private OnkyoIscpCommand(eOnkyoCommand command, bool parameter)
        {
            m_Command = command;
            m_Parameter = GetBoolParameter(parameter);
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

        public string GetEthernetHeader()
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

        #region Commands

        private static OnkyoIscpCommand GetQuery(eOnkyoCommand command)
        {
            return new OnkyoIscpCommand(command, QUERY_PARAMETER);
        }

        public static OnkyoIscpCommand PowerCommand(bool power)
        {
            return new OnkyoIscpCommand(eOnkyoCommand.Power, power);
        }

        public static OnkyoIscpCommand PowerQuery()
        {
            return GetQuery(eOnkyoCommand.Power);
        }

        public static OnkyoIscpCommand MuteCommand(bool mute)
        {
            return new OnkyoIscpCommand(eOnkyoCommand.Mute, mute);
        }

        public static OnkyoIscpCommand MuteToggle()
        {
            return new OnkyoIscpCommand(eOnkyoCommand.Mute, TOGGLE_PARAMETER);
        }

        public static OnkyoIscpCommand MuteQuery()
        {
            return GetQuery(eOnkyoCommand.Mute);
        }
        
        private static OnkyoIscpCommand VolumeSet(string volume)
        {
            return new OnkyoIscpCommand(eOnkyoCommand.Volume, volume);
        }

        public static OnkyoIscpCommand VolumeIncrement()
        {
            return VolumeSet(UP_PARAMETER);
        }

        public static OnkyoIscpCommand VolumeDecrement()
        {
            return VolumeSet(DOWN_PARAMETER);
        }

        public static OnkyoIscpCommand VolumeSet(int volume)
        {
            return VolumeSet(string.Format("{0:X2}", volume));
        }

        public static OnkyoIscpCommand VolumeQuery()
        {
            return GetQuery(eOnkyoCommand.Volume);
        }

        public static OnkyoIscpCommand InputSet(int number)
        {
            return new OnkyoIscpCommand(eOnkyoCommand.Input, string.Format("{0:X2}", number));
        }

        public static OnkyoIscpCommand InputQuery()
        {
            return GetQuery(eOnkyoCommand.Input);
        }

        public static OnkyoIscpCommand ReceiverInformationQuery()
        {
            return GetQuery(eOnkyoCommand.ReceiverInformation);
        }

        #endregion
    }
}