using System;

namespace ICD.Connect.Audio.Controls
{
    public sealed class VolumeDeviceVolumeChangedEventArgs : EventArgs
    {
		public float VolumeRaw { get; private set; }
		public float VolumePosition { get; private set; }
		public string VolumeString { get; private set; }

	    public VolumeDeviceVolumeChangedEventArgs(float volumeRaw, float volumePosition, string volumeString)
	    {
		    VolumeRaw = volumeRaw;
		    VolumePosition = volumePosition;
		    VolumeString = volumeString;
	    }
    }
}
