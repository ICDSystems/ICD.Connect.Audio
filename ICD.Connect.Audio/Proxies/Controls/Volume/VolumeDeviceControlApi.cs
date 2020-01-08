namespace ICD.Connect.Audio.Proxies.Controls.Volume
{
	public static class VolumeDeviceControlApi
	{
		public const string EVENT_VOLUME_CHANGED = "OnVolumeChanged";
		public const string EVENT_IS_MUTED_CHANGED = "OnIsMutedChanged";
		public const string EVENT_SUPPORTED_VOLUME_FEATURES_CHANGED = "OnSupportedVolumeFeaturesChanged";

		public const string PROPERTY_SUPPORTED_VOLUME_FEATURES = "SupportedVolumeFeatures";
		public const string PROPERTY_IS_MUTED = "IsMuted";
		public const string PROPERTY_VOLUME_LEVEL = "VolumeLevel";
		public const string PROPERTY_VOLUME_STRING = "VolumeString";
		public const string PROPERTY_VOLUME_LEVEL_MAX = "VolumeLevelMax";
		public const string PROPERTY_VOLUME_LEVEL_MIN = "VolumeLevelMin";

		public const string METHOD_SET_VOLUME_LEVEL = "SetVolumeLevel";
		public const string METHOD_SET_IS_MUTED = "SetIsMuted";
		public const string METHOD_TOGGLE_IS_MUTED = "VolumeMuteToggle";
		public const string METHOD_VOLUME_INCREMENT = "VolumeLevelIncrement";
		public const string METHOD_VOLUME_DECREMENT = "VolumeLevelDecrement";
		public const string METHOD_VOLUME_RAMP = "VolumeRamp";
		public const string METHOD_VOLUME_RAMP_STOP = "VolumeRampStop";

		public const string HELP_EVENT_VOLUME_CHANGED = "Raised when the volume changes.";
		public const string HELP_EVENT_IS_MUTED_CHANGED = "Raised when the mute state changes.";
		public const string HELP_EVENT_SUPPORTED_VOLUME_FEATURES_CHANGED = "Raised when the supported volume features change.";

		public const string HELP_PROPERTY_SUPPORTED_VOLUME_FEATURES = "Returns true if the control will raise feedback for the current mute state.";
		public const string HELP_PROPERTY_VOLUME_LEVEL = "Gets the current volume, in the parent device's format.";
		public const string HELP_PROPERTY_VOLUME_STRING = "Gets the current volume, in string representation.";
		public const string HELP_PROPERTY_VOLUME_LEVEL_MAX = "Maximum value for the volume level.";
		public const string HELP_PROPERTY_VOLUME_LEVEL_MIN = " Minimum value for the volume level.";
		public const string HELP_PROPERTY_IS_MUTED = "Gets the muted state.";

		public const string HELP_METHOD_VOLUME_INCREMENT = "Raises the volume one time.";
		public const string HELP_METHOD_VOLUME_DECREMENT = "Lowers the volume one time.";
		public const string HELP_METHOD_VOLUME_RAMP = "Starts raising the volume, and continues until stop is called.";
		public const string HELP_METHOD_VOLUME_RAMP_STOP = "Stops any current ramp in progress.";
		public const string HELP_METHOD_SET_VOLUME_LEVEL = "Sets the volume level. This will be clamped to the min/max.";
		public const string HELP_METHOD_SET_IS_MUTED = "Sets the mute state.";
		public const string HELP_METHOD_TOGGLE_IS_MUTED = "Toggles the current mute state.";
	}
}
