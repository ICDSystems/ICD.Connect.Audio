namespace ICD.Connect.Audio.Proxies.Controls.Microphone
{
	public static class MicrophoneDeviceControlApi
	{
		public const string EVENT_GAIN_LEVEL_CHANGED = "GainLevelChanged";
		public const string EVENT_PHANTOM_POWER_STATE_CHANGED = "PhantomPowerStateChanged";
		public const string EVENT_MUTE_STATE_CHANGED = "MuteStateChanged";

		public const string PROPERTY_GAIN_LEVEL = "GainLevel";
		public const string PROPERTY_IS_MUTED = "IsMuted";
		public const string PROPERTY_PHANTOM_POWER = "PhantomPower";

		public const string METHOD_SET_PHANTOM_POWER = "SetPhantomPower";
		public const string METHOD_SET_GAIN_LEVEL = "SetGainLevel";
		public const string METHOD_SET_MUTED = "SetMuted";
		public const string METHOD_MUTE_TOGGLE = "MuteToggle";
		public const string METHOD_PHANTOM_POWER_TOGGLE = "PhantomPowerToggle";

		public const string HELP_EVENT_GAIN_LEVEL_CHANGED = "Raised when the gain level changes.";
		public const string HELP_EVENT_MUTE_STATE_CHANGED = "Raised when the mute state changes.";
		public const string HELP_EVENT_PHANTOM_POWER_STATE_CHANGED = "Raised when the phantom power state changes.";

		public const string HELP_PROPERTY_GAIN_LEVEL = "Gets the gain level.";
		public const string HELP_PROPERTY_IS_MUTED = "Gets the muted state.";
		public const string HELP_PROPERTY_PHANTOM_POWER = "Gets the phantom power state.";

		public const string HELP_METHOD_SET_PHANTOM_POWER = "Sets the phantom power state.";
		public const string HELP_METHOD_SET_GAIN_LEVEL = "Sets the gain level.";
		public const string HELP_METHOD_SET_MUTED = "Sets the muted state.";
		public const string HELP_METHOD_MUTE_TOGGLE = "Toggles the current mute state.";
		public const string HELP_METHOD_PHANTOM_POWER_TOGGLE = "Toggles the current phantom power state.";
	}
}
