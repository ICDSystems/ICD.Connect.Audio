using ICD.Connect.Audio.Biamp.AttributeInterfaces;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	/// <summary>
	/// Wraps a logic block to provide a simple on/off switch.
	/// </summary>
	public sealed class BiampTesiraStateDeviceControl : AbstractBiampTesiraStateDeviceControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="stateAttribute"></param>
		public BiampTesiraStateDeviceControl(int id, string name, IStateAttributeInterface stateAttribute)
			: base(id, name, stateAttribute)
		{
		}
	}
}
