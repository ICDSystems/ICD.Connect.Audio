using ICD.Connect.Devices.Mock;
using ICD.Connect.Routing.Mock.Midpoint;

namespace ICD.Connect.Audio.Mock.Devices
{
	/// <summary>
	/// Mock device with routing and volume features.
	/// </summary>
	public sealed class MockAudioDevice : AbstractMockDevice<MockAudioDeviceSettings>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MockAudioDevice()
		{
			Controls.Add(new MockRouteMidpointControl(this, 0));
			Controls.Add(new MockAudioDeviceVolumeControl(this, 1));
		}
	}
}
