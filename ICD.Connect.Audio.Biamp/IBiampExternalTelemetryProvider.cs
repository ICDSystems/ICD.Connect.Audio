using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Telemetry;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Audio.Biamp
{
	public interface IBiampExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[EventTelemetry(DspTelemetryNames.ACTIVE_FAULT_STATE_CHANGED)]
		event EventHandler<BoolEventArgs> OnActiveFaultStateChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION_CHANGED)]
		event EventHandler<StringEventArgs> OnFirmwareVersionChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_ADDRESS_CHANGED)]
		event EventHandler<StringEventArgs> OnIpAddressChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_SUBNET_CHANGED)]
		event EventHandler<StringEventArgs> OnIpSubnetChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_GATEWAY_CHANGED)]
		event EventHandler<StringEventArgs> OnIpGatewayChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_HOSTNAME_CHANGED)]
		event EventHandler<StringEventArgs> OnHostnameChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_MAC_ADDRESS_CHANGED)]
		event EventHandler<StringEventArgs> OnMacAddressChanged;

		[EventTelemetry(DspTelemetryNames.ACTIVE_FAULT_MESSAGE_CHANGED)]
		event EventHandler<StringEventArgs> OnActiveFaultMessagesChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_SERIAL_NUMBER_CHANGED)]
		event EventHandler<StringEventArgs> OnSerialNumberChanged;

		[PropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_STATE, null, DspTelemetryNames.ACTIVE_FAULT_STATE_CHANGED)]
		bool ActiveFaultState { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION, null, DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION_CHANGED)]
		string FirmwareVersion { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_ADDRESS, null, DeviceTelemetryNames.DEVICE_IP_ADDRESS_CHANGED)]
		string IpAddress { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_SUBNET, null, DeviceTelemetryNames.DEVICE_IP_SUBNET_CHANGED)]
		string IpSubnet { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_GATEWAY, null, DeviceTelemetryNames.DEVICE_IP_GATEWAY_CHANGED)]
		string IpGateway { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_HOSTNAME, null, DeviceTelemetryNames.DEVICE_HOSTNAME_CHANGED)]
		string Hostname { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_MAC_ADDRESS, null, DeviceTelemetryNames.DEVICE_MAC_ADDRESS_CHANGED)]
		string MacAddress { get; }

		[PropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_MESSAGE, null, DspTelemetryNames.ACTIVE_FAULT_MESSAGE_CHANGED)]
		string ActiveFaultMessages { get; }

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_SERIAL_NUMBER, null, DeviceTelemetryNames.DEVICE_SERIAL_NUMBER_CHANGED)]
		string SerialNumber { get; }
	}
}