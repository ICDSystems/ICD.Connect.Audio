using ICD.Common.Utils.Collections;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public enum eOnkyoCommand
    {
        Power,
        Mute,
        Volume,
        Input,
        ReceiverInformation,
        Zone2Power,
        Zone2Mute,
        Zone2Volume,
        Zone2Input,
        Zone3Power,
        Zone3Mute,
        Zone3Volume,
        Zone3Input,

    }

    public static class OnkyoCommandExtensions
    {
        public static string GetStringForCommand(this eOnkyoCommand extends)
        {
            return OnkyoCommandUtils.GetStringForCommand(extends);
        }
    }

    public static class OnkyoCommandUtils
    {
        private static readonly BiDictionary<eOnkyoCommand, string> s_CommandStrings =
            new BiDictionary<eOnkyoCommand, string>
            {
                { eOnkyoCommand.Power, "PWR" },
                { eOnkyoCommand.Mute, "AMT" },
                { eOnkyoCommand.Volume, "MVL" },
                { eOnkyoCommand.Input, "SLI" },
                { eOnkyoCommand.ReceiverInformation, "NRI" },
                { eOnkyoCommand.Zone2Power, "ZPW" },
                { eOnkyoCommand.Zone2Mute, "ZMT" },
                { eOnkyoCommand.Zone2Volume, "ZVL" },
                { eOnkyoCommand.Zone2Input, "SLZ" },
                { eOnkyoCommand.Zone3Power , "PW3" },
                { eOnkyoCommand.Zone3Mute , "MT3"},
                { eOnkyoCommand.Zone3Volume , "VL3"},
                { eOnkyoCommand.Zone3Input, "SL3"}
            };
        public static string GetStringForCommand(eOnkyoCommand command)
        {
            return s_CommandStrings.GetValue(command);
        }

        public static bool TryGetCommandForString(string commandString, out eOnkyoCommand command)
        {
            return s_CommandStrings.TryGetKey(commandString, out command);
        }
    }
}