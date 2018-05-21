using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
	public interface INamedComponentControl : IConsoleNode
	{
		string Name { get; }

		event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		void ParseFeedback(JToken feedback);

		void SetValue(string value);
		void SetPosition(float position);
		void TriggerControl();
	}
}