using System.Collections.Generic;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.CoreControls.ChangeGroups
{
	public interface IChangeGroup : IQSysCoreControl
	{
		string ChangeGroupId { get; }
		void AddNamedControl(INamedControl control);
		void AddNamedControl(IEnumerable<INamedControl> controls);
		void Initialize();
		void DestroyChangeGroup();

		// Also needs to implement constructors like:
		// IChangeGroup(int id, string friendlyName, CoreElementsLoadContext context, string xml);
		// and
		// IChangeGroup(int id, CoreElementsLoadContext context, string changeGroupId);
	}
}