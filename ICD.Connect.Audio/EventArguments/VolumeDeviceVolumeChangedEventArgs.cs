using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Audio.Proxies.Controls;

namespace ICD.Connect.Audio.EventArguments
{
	[Serializable]
	public sealed class VolumeChangeState
	{
		public float VolumeRaw { get; set; }

		public float VolumePosition { get; set; }

		public string VolumeString { get; set; }
	}

	public sealed class VolumeDeviceVolumeChangedEventArgs : AbstractGenericApiEventArgs<VolumeChangeState>
	{
		public float VolumeRaw { get { return Data.VolumeRaw; } }

		public float VolumePosition { get { return Data.VolumePosition; } }

		public string VolumeString { get { return Data.VolumeString; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="volumeRaw"></param>
		/// <param name="volumePosition"></param>
		/// <param name="volumeString"></param>
		public VolumeDeviceVolumeChangedEventArgs(float volumeRaw, float volumePosition, string volumeString)
			: base(VolumeLevelDeviceControlApi.EVENT_VOLUME_CHANGED,
			       new VolumeChangeState
			       {
				       VolumeRaw = volumeRaw,
				       VolumePosition = volumePosition,
				       VolumeString = volumeString
			       })
		{
		}
	}
}
