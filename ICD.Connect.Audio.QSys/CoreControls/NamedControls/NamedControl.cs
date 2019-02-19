using ICD.Connect.Audio.QSys.Controls;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedControls
{
    /// <summary>
    /// Represents a generic named control
    /// </summary>
    public sealed class NamedControl : AbstractNamedControl
    {
		public NamedControl(int id, CoreElementsLoadContext loadContext, string controlName)
			: base(id, loadContext, controlName)
        {}

		public NamedControl(int id, string name, CoreElementsLoadContext loadContext, string xml):base(id,name,loadContext,xml)
		{}
    }
}
