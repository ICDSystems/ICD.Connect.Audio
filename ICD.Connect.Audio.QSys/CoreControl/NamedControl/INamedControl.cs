namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
    public interface INamedControl
    {
        string ControlName { get; }
        float ValuePosition { get; }
        string ValueString { get; }
        float ValueValue { get; }

        void PollValue();
        void SetValue(object value);
        void SetFeedback(string valueString, float valueValue, float valuePosition);
    }
}