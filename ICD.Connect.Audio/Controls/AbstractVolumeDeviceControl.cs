using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls
{
	public abstract class AbstractVolumeDeviceControl<T> : AbstractDeviceControl<T>, IVolumeDeviceControl
		where T : IDeviceBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractVolumeDeviceControl(T parent, int id)
			: base(parent, id)
		{
		}
	}
}
