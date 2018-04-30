using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Devices
{
	/// <summary>
	/// The GenericAmpDevice is essentially a mock audio switcher that provides a single
	/// volume control, representing the nearest, currently routed volume control.
	/// </summary>
	public sealed class GenericAmpDevice : AbstractDevice<GenericAmpDeviceSettings>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public GenericAmpDevice()
		{
			Controls.Add(new GenericAmpRouteSwitcherControl(this, 0));

			// Needs to be added after the route control
			Controls.Add(new GenericAmpVolumeControl(this, 1));
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}
	}
}
