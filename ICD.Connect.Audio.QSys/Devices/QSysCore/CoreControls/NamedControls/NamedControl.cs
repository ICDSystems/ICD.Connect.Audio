using ICD.Common.Properties;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls
{
	/// <summary>
	/// Represents a generic named control
	/// </summary>
	public sealed class NamedControl : AbstractNamedControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="loadContext"></param>
		/// <param name="controlName"></param>
		[UsedImplicitly]
		public NamedControl(int id, CoreElementsLoadContext loadContext, string controlName)
			: base(id, loadContext, controlName)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="loadContext"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public NamedControl(int id, string name, CoreElementsLoadContext loadContext, string xml)
			: base(id, name, loadContext, xml)
		{
		}
	}
}
