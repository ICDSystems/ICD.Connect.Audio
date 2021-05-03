using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.QSys.EventArgs
{
    public sealed class ControlValueUpdateEventArgs : GenericEventArgs<ControlValueUpdateEventData>
    {
	    public string ControlName { get { return Data.ControlName; } }

	    public string ValueString { get { return Data.ValueString; } }

		public float ValueRaw { get { return Data.ValueRaw; } }

		public float ValuePosition { get { return Data.ValuePosition;} }

	    public ControlValueUpdateEventArgs(string controlName, string valueString, float valueRaw, float valuePosition):this(new ControlValueUpdateEventData(controlName, valueString, valueRaw, valuePosition))
	    {
	    }

	    public ControlValueUpdateEventArgs(ControlValueUpdateEventData data) : base(data)
	    {
	    }
    }

	public sealed class ControlValueUpdateEventData
	{
		public string ControlName { get; private set; }

		public string ValueString { get; private set; }

		public float ValueRaw { get; private set; }

		public float ValuePosition { get; private set; }

		public ControlValueUpdateEventData(string controlName, string valueString, float valueRaw, float valuePosition)
		{
			ControlName = controlName;
			ValueString = valueString;
			ValueRaw = valueRaw;
			ValuePosition = valuePosition;
		}
	}
}
