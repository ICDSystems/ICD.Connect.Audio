using System;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Audio.Proxies.Controls.Volume;

namespace ICD.Connect.Audio.EventArguments
{
	[Serializable]
	public sealed class VolumeChangeState
	{
		public float VolumeLevel { get; set; }

		public float VolumePercent { get; set; }

		public string VolumeString { get; set; }
	}

	public sealed class VolumeControlVolumeChangedApiEventArgs : AbstractGenericApiEventArgs<VolumeChangeState>
	{
		public float VolumeLevel { get { return Data.VolumeLevel; } }

		public float VolumePercent { get { return Data.VolumePercent; } }

		public string VolumeString { get { return Data.VolumeString; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="volumeRaw"></param>
		/// <param name="volumePercent"></param>
		/// <param name="volumeString"></param>
		public VolumeControlVolumeChangedApiEventArgs(float volumeRaw, float volumePercent, string volumeString)
			: base(VolumeDeviceControlApi.EVENT_VOLUME_CHANGED,
			       new VolumeChangeState
			       {
				       VolumeLevel = volumeRaw,
				       VolumePercent = volumePercent,
				       VolumeString = volumeString
			       })
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="state"></param>
		public VolumeControlVolumeChangedApiEventArgs(VolumeChangeState state)
			: base(VolumeDeviceControlApi.EVENT_VOLUME_CHANGED, state)
		{			
		}
	}
}
