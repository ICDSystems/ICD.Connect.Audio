namespace ICD.Connect.Audio.Proxies.Controls.Volume
{
	public static class VolumeLevelDeviceControlApi
	{
		public const string EVENT_VOLUME_CHANGED = "OnVolumeChanged";

		public const string PROPERTY_VOLUME_LEVEL = "VolumeLevel";
		public const string PROPERTY_VOLUME_PERCENT = "VolumePercent";
		public const string PROPERTY_VOLUME_STRING = "VolumeString";
		public const string PROPERTY_VOLUME_LEVEL_MAX = "VolumeLevelMax";
		public const string PROPERTY_VOLUME_LEVEL_MIN = "VolumeLevelMin";

		public const string METHOD_SET_VOLUME_LEVEL = "SetVolumeLevel";
		public const string METHOD_SET_VOLUME_PERCENT = "SetVolumePercent";
		public const string METHOD_VOLUME_LEVEL_RAMP_PERCENT_UP = "VolumeLevelRampPercentUp";
		public const string METHOD_VOLUME_LEVEL_RAMP_PERCENT_DOWN = "VolumeLevelRampPercentDown";

		public const string HELP_EVENT_VOLUME_CHANGED = "Raised when the volume changes.";

		public const string HELP_PROPERTY_VOLUME_LEVEL = "Gets the current volume, in the parent device's format.";
		public const string HELP_PROPERTY_VOLUME_PERCENT = "Gets the current volume percent, 0 - 1.";
		public const string HELP_PROPERTY_VOLUME_STRING = "Gets the current volume, in string representation.";
		public const string HELP_PROPERTY_VOLUME_LEVEL_MAX = "Maximum value for the volume level.";
		public const string HELP_PROPERTY_VOLUME_LEVEL_MIN = " Minimum value for the volume level.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_PERCENT_UP = "Starts raising the volume in steps of the given percent, and continues until RampStop is called.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_PERCENT_DOWN = "Starts lowering the volume in steps of the given percent, and continues until RampStop is called.";
		
		public const string HELP_METHOD_SET_VOLUME_LEVEL = "Sets the volume level. This will be clamped to the min/max.";
		public const string HELP_METHOD_SET_VOLUME_PERCENT = "Sets the volume percent, from 0-1.";
	}
}
