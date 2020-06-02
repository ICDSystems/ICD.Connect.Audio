using System;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Mock;
using ICD.Connect.Routing.Mock.Midpoint;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Mock.Devices
{
	/// <summary>
	/// Mock device with routing and volume features.
	/// </summary>
	public sealed class MockAudioDevice : AbstractMockDevice<MockAudioDeviceSettings>
	{
		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(MockAudioDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new MockRouteMidpointControl(this, 0));
			addControl(new MockAudioDeviceVolumeControl(this, 1));
		}
	}
}
