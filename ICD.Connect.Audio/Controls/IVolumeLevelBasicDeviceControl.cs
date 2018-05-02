namespace ICD.Connect.Audio.Controls
{
	/// <summary>
	/// Basic volume control for devices
	/// Supports Up/Down
	/// Designed to support IR controlled devices
	/// </summary>
    public interface IVolumeLevelBasicDeviceControl : IVolumeDeviceControl
    {
		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
	    void VolumeLevelIncrement();

	    /// <summary>
	    /// Lowers the volume one time
	    /// Amount of the change varies between implementations - typically "1" raw unit
	    /// </summary>
		void VolumeLevelDecrement();

	    /// <summary>
	    /// Starts raising the volume, and continues until RampStop is called.
	    /// <see cref="VolumeLevelRampStop"/> must be called after
	    /// </summary>
	    void VolumeLevelRampUp();

	    /// <summary>
	    /// Starts lowering the volume, and continues until RampStop is called.
	    /// <see cref="VolumeLevelRampStop"/> must be called after
	    /// </summary>
	    void VolumeLevelRampDown();

	    /// <summary>
	    /// Stops any current ramp up/down in progress.
	    /// </summary>
	    void VolumeLevelRampStop();
	}
}
