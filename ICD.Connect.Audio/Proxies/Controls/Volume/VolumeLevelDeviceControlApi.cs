namespace ICD.Connect.Audio.Proxies.Controls.Volume
{
	public static class VolumeLevelDeviceControlApi
	{
		public const string EVENT_VOLUME_CHANGED = "OnVolumeChanged";

		public const string PROPERTY_VOLUME_LEVEL = "VolumeRaw";
		public const string PROPERTY_VOLUME_POSITION = "VolumePosition";
		public const string PROPERTY_VOLUME_STRING = "VolumeString";
		public const string PROPERTY_VOLUME_RAW_MAX = "VolumeRawMax";
		public const string PROPERTY_VOLUME_RAW_MIN = "VolumeRawMin";

		public const string METHOD_SET_VOLUME_LEVEL = "SetVolumeRaw";
		public const string METHOD_SET_VOLUME_POSITION = "SetVolumePosition";
		public const string METHOD_VOLUME_LEVEL_INCREMENT = "VolumeLevelIncrement";
		public const string METHOD_VOLUME_LEVEL_DECREMENT = "VolumeLevelDecrement";
		public const string METHOD_VOLUME_LEVEL_RAMP_POSITION_UP = "VolumeLevelRamppPositionUp";
		public const string METHOD_VOLUME_LEVEL_RAMP_POSITION_DOWN = "VolumeLevelRampPositionDown";

		public const string HELP_EVENT_VOLUME_CHANGED = "Raised when the raw volume changes.";

		public const string HELP_PROPERTY_VOLUME_RAW = "Gets the current volume, in the parent device's format.";
		public const string HELP_PROPERTY_VOLUME_POSITION = "Gets the current volume positon, 0 - 1.";
		public const string HELP_PROPERTY_VOLUME_STRING = "Gets the current volume, in string representation.";
		public const string HELP_PROPERTY_VOLUME_RAW_MAX = "Maximum value for the raw volume level.";
		public const string HELP_PROPERTY_VOLUME_RAW_MIN = " Minimum value for the raw volume level.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_POSITION_UP = "Starts raising the volume in steps of the given position, and continues until RampStop is called.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_POSITION_DOWN = "Starts lowering the volume in steps of the given position, and continues until RampStop is called.";
		
		public const string HELP_METHOD_SET_VOLUME_LEVEL = "Sets the raw volume. This will be clamped to the min/max and safety min/max.";
		public const string HELP_METHOD_SET_VOLUME_POSITION = "Sets the volume position, from 0-1.";
		public const string HELP_METHOD_VOLUME_LEVEL_INCREMENT = " Increments the volume once.";
		public const string HELP_METHOD_VOLUME_LEVEL_DECREMENT = "Decrements the volume once.";
	}
}
