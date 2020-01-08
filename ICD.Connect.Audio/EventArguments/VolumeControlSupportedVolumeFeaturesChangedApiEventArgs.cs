using ICD.Connect.API.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.Proxies.Controls.Volume;

namespace ICD.Connect.Audio.EventArguments
{
	public sealed class VolumeControlSupportedVolumeFeaturesChangedApiEventArgs : AbstractGenericApiEventArgs<eVolumeFeatures>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public VolumeControlSupportedVolumeFeaturesChangedApiEventArgs(eVolumeFeatures data)
			: base(VolumeDeviceControlApi.EVENT_SUPPORTED_VOLUME_FEATURES_CHANGED, data)
		{
		}
	}
}
