using ICD.Connect.Devices;
using ICD.Connect.Routing.Mock.Midpoint;

namespace ICD.Connect.Audio.Mock.Devices
{
	/// <summary>
	/// Mock device with routing and volume features.
	/// </summary>
	public sealed class MockAudioDevice : AbstractDevice<MockAudioDeviceSettings>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MockAudioDevice()
		{
			Controls.Add(new MockRouteMidpointControl(this, 0));
			Controls.Add(new MockAudioDeviceVolumeControl(this, 1));
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
