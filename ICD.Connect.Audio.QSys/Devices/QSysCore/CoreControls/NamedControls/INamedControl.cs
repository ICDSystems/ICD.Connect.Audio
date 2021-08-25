#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json.Linq;
#else
using Newtonsoft.Json.Linq;
#endif
using System;
using ICD.Common.Utils;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.EventArgs;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls
{
	public interface INamedControl : IQSysCoreControl, IConsoleNode, IStateDisposable
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

		// Also needs to implement constructors like:
		// INamedControl(int id, string friendlyName, CoreElementsLoadContext context, string xml);
		// and
		// INamedControl(int id, CoreElementsLoadContext context, string controlName);
	}
}