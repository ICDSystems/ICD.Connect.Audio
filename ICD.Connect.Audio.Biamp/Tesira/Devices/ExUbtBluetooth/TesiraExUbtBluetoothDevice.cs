﻿using System;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.ExUbt;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Misc.Bluetooth;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices.ExUbtBluetooth
{
	public sealed class TesiraExUbtBluetoothDevice : AbstractTesiraChildAttributeInterfaceDevice<ExUbtBluetoothControlStatusBlock,TesiraExUbtBluetoothDeviceSettings>, IBluetoothDevice
	{
		private const int BLUETOOTH_CONTROL_ID = 1;
		private const int CONFERENCE_CONTROL_ID = 2;

		public ExUbtBluetoothControlStatusBlock BluetoothControlStatusBlock {get { return AttributeInterface; }}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(TesiraExUbtBluetoothDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new TesiraExUbtBluetoothControl(this, BLUETOOTH_CONTROL_ID));
			addControl(new TesiraExUbtBluetoothConferenceControl(this, CONFERENCE_CONTROL_ID));
		}

		protected override void Subscribe(ExUbtBluetoothControlStatusBlock attributeInterface)
		{
			base.Subscribe(attributeInterface);

			if (attributeInterface == null)
				return;
		}

		protected override void Unsubscribe(ExUbtBluetoothControlStatusBlock attributeInterface)
		{
			base.Unsubscribe(attributeInterface);

			if (attributeInterface == null)
				return;
		}

		protected override void SetAttributeInterface(ExUbtBluetoothControlStatusBlock attributeInterface)
		{
			base.SetAttributeInterface(attributeInterface);

			IDeviceControl btControl;
			if (Controls.TryGetControl(BLUETOOTH_CONTROL_ID, out btControl) && btControl is TesiraExUbtBluetoothControl)
			{
				((TesiraExUbtBluetoothControl)btControl).SetBlock(attributeInterface);
			}

			IDeviceControl confControl;
			if (Controls.TryGetControl(CONFERENCE_CONTROL_ID, out confControl) && confControl is TesiraExUbtBluetoothConferenceControl)
			{
				((TesiraExUbtBluetoothConferenceControl)confControl).SetBlock(attributeInterface);
			}
		}
	}
}