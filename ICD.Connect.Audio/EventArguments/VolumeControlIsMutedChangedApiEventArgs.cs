using ICD.Connect.API.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Volume;

namespace ICD.Connect.Audio.EventArguments
{
	public sealed class VolumeControlIsMutedChangedApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public VolumeControlIsMutedChangedApiEventArgs(bool data)
			: base(VolumeDeviceControlApi.EVENT_IS_MUTED_CHANGED, data)
		{
		}
	}
}