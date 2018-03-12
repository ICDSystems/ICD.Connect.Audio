using System;
using ICD.Connect.Devices.Controls;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControl.NamedControl
{
	public interface INamedControl : IDeviceControl
	{
		string ControlName { get; }
		float ValuePosition { get; }
		float ValueRaw { get; }
		string ValueString { get; }

		event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		void ParseFeedback(JToken feedback);
		void PollValue();
		void SetPosition(float position);
		void SetValue(object value);
		void TriggerControl();
	}
}