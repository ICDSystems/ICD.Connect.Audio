namespace ICD.Connect.Audio.Proxies.Controls
{
    public static class VolumeLevelBasicDeviceControlApi
    {
		public const string METHOD_VOLUME_LEVEL_INCREMENT = "VolumeLevelIncrement";
		public const string METHOD_VOLUME_LEVEL_DECREMENT = "VolumeLevelDecrement";
		public const string METHOD_VOLUME_LEVEL_RAMP_UP = "VolumeLevelRampUp";
		public const string METHOD_VOLUME_LEVEL_RAMP_DOWN = "VolumeLevelRampDown";
		public const string METHOD_VOLUME_LEVEL_RAMP_STOP = "VolumeLevelRampStop";

		public const string HELP_METHOD_VOLUME_LEVEL_INCREMENT = "Raises the volume one time.";
		public const string HELP_METHOD_VOLUME_LEVEL_DECREMENT = "Lowers the volume one time.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_UP = "Starts raising the volume, and continues until RampStop is called.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_DOWN = "Starts lowering the volume, and continues until RampStop is called.";
		public const string HELP_METHOD_VOLUME_LEVEL_RAMP_STOP = "Stops any current ramp up/down in progress.";
	}
}
