using ICD.Common.Utils.Collections;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public enum eOnkyoCommand
    {
        Power,
        Mute,
        Volume,
        Input,
        ReceiverInformation
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
        private static BiDictionary<eOnkyoCommand, string> s_CommandStrings = new BiDictionary<eOnkyoCommand, string>
        {
            {eOnkyoCommand.Power, "PWR"},
            {eOnkyoCommand.Mute, "AMT"},
            {eOnkyoCommand.Volume, "MVL"},
            {eOnkyoCommand.Input, "SLI"},
            { eOnkyoCommand.ReceiverInformation , "NRI"}
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