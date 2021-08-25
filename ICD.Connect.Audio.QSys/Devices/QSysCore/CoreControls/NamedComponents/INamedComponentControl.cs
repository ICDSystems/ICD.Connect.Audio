#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json.Linq;
#else
using Newtonsoft.Json.Linq;
#endif
using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.EventArgs;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public interface INamedComponentControl : IConsoleNode
	{
		string Name { get; }

		string ValueString { get; }

		float ValueRaw { get; }

		float ValuePosition { get; }

		event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		void ParseFeedback(JToken feedback);

		void SetValue(string value);
		void SetValue(bool value);
		void SetPosition(float position);
		void Trigger();
	}
}