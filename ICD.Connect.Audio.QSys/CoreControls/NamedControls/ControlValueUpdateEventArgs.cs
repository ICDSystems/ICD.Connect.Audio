using System;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedControls
{
    public sealed class ControlValueUpdateEventArgs : EventArgs
    {
		public string ControlName { get; private set; }

		public string ValueString { get; private set; }

		public float ValueRaw { get; private set; }

		public float ValuePosition { get; private set; }

	    public ControlValueUpdateEventArgs(string controlName, string valueString, float valueRaw, float valuePostion)
	    {
		    ControlName = controlName;
		    ValueString = valueString;
		    ValueRaw = valueRaw;
		    ValuePosition = valuePostion;
	    }
    }
}
