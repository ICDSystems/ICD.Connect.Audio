using ICD.Connect.API.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Mute;

namespace ICD.Connect.Audio.EventArguments
{
	public sealed class MuteDeviceMuteStateChangedApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public MuteDeviceMuteStateChangedApiEventArgs(bool data)
			: base(VolumeMuteFeedbackDeviceControlApi.EVENT_MUTE_STATE_CHANGED, data)
		{
		}
	}
}