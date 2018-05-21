using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
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

    }
}
