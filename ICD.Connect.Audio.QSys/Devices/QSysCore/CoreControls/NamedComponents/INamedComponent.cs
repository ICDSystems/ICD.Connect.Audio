#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json.Linq;
#else
using Newtonsoft.Json.Linq;
#endif
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public interface INamedComponent : IQSysCoreControl, IConsoleNode, IStateDisposable
    {

		/// <summary>
		/// Name of the component in QSys
		/// </summary>
		string ComponentName { get; }

		/// <summary>
		/// Method called for the component to parse feedback from qsys
		/// </summary>
		/// <param name="result"></param>
	    void ParseFeedback(JToken result);

		IEnumerable<INamedComponentControl> GetControls();

		// Also needs to implement constructors like:
		// INamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml);
		// and
		// INamedComponent(int id, CoreElementsLoadContext context, string componentName);

		void SetValue(string controlName, string value);

		void Trigger(string controlName);

		/// <summary>
		/// Method called for the component to parse property feedback form qsys
		/// </summary>
		/// <param name="property"></param>
		void ParsePropertyFeedback(JToken property);
    }
}
