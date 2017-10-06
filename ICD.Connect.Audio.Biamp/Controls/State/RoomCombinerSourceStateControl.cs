using ICD.Connect.Audio.Biamp.AttributeInterfaces;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	public sealed class RoomCombinerSourceStateControl : AbstractBiampTesiraStateDeviceControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="stateAttribute"></param>
		public RoomCombinerSourceStateControl(int id, string name, IStateAttributeInterface stateAttribute)
			: base(id, name, stateAttribute)
		{
		}
	}
}
