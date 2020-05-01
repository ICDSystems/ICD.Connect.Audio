using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.Services;
using ICD.Connect.Devices;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Biamp
{
	public sealed class BiampExternalTelemetryProvider : IBiampExternalTelemetryProvider
	{
		private const string NO_FAULTS_MESSAGE = "No fault in device";

		public event EventHandler<BoolEventArgs> OnActiveFaultStateChanged;
		public event EventHandler<StringEventArgs> OnFirmwareVersionChanged;
		public event EventHandler<StringEventArgs> OnIpAddressChanged;
		public event EventHandler<StringEventArgs> OnIpSubnetChanged;
		public event EventHandler<StringEventArgs> OnIpGatewayChanged;
		public event EventHandler<StringEventArgs> OnHostnameChanged;
		public event EventHandler<StringEventArgs> OnMacAddressChanged;
		public event EventHandler<StringEventArgs> OnActiveFaultMessagesChanged;
		public event EventHandler<StringEventArgs> OnSerialNumberChanged;

		private BiampTesiraDevice m_Parent;
		private bool m_ActiveFaultState;
		private string m_FirmwareVersion;
		private string m_IpAddress;
		private string m_Hostname;
		private string m_MacAddress;
		private string m_ActiveFaultMessages;
		private string m_SerialNumber;
		private string m_IpSubnet;
		private string m_IpGateway;

		public bool ActiveFaultState
		{
			get { return m_ActiveFaultState; }
			private set
			{
				if (m_ActiveFaultState == value)
					return;

				m_ActiveFaultState = value;

				OnActiveFaultStateChanged.Raise(this, new BoolEventArgs(m_ActiveFaultState));
			}
		}

		public string FirmwareVersion
		{
			get { return m_FirmwareVersion; }
			private set
			{
				if (m_FirmwareVersion == value)
					return;

				m_FirmwareVersion = value;

				OnFirmwareVersionChanged.Raise(this, new StringEventArgs(m_FirmwareVersion));
			}
		}

		public string IpAddress
		{
			get { return m_IpAddress; }
			private set
			{
				if (m_IpAddress == value)
					return;

				m_IpAddress = value;

				OnIpAddressChanged.Raise(this, new StringEventArgs(m_IpAddress));
			}
		}

		public string IpSubnet
		{
			get { return m_IpSubnet; }
			private set
			{
				if (m_IpSubnet == value)
					return;

				m_IpSubnet = value;

				OnIpSubnetChanged.Raise(this, new StringEventArgs(m_IpSubnet));
			}
		}

		public string IpGateway
		{
			get { return m_IpGateway; }
			private set
			{
				if (m_IpGateway == value)
					return;

				m_IpGateway = value;

				OnIpGatewayChanged.Raise(this, new StringEventArgs(m_IpGateway));
			}
		}

		public string Hostname
		{
			get { return m_Hostname; }
			private set
			{
				if (m_Hostname == value)
					return;

				m_Hostname = value;

				OnHostnameChanged.Raise(this, new StringEventArgs(m_Hostname));
			}
		}

		public string MacAddress
		{
			get { return m_MacAddress; }
			private set
			{
				if (m_MacAddress == value)
					return;

				m_MacAddress = value;

				OnMacAddressChanged.Raise(this, new StringEventArgs(m_MacAddress));
			}
		}

		public string ActiveFaultMessages
		{
			get { return m_ActiveFaultMessages; } 
			private set
			{
				if (m_ActiveFaultMessages == value)
					return;

				m_ActiveFaultMessages = value;

				OnActiveFaultMessagesChanged.Raise(this, new StringEventArgs(m_ActiveFaultMessages));
			}
		}

		public string SerialNumber
		{
			get { return m_SerialNumber;}
			private set
			{
				if (m_SerialNumber == value)
					return;

				m_SerialNumber = value;

				OnSerialNumberChanged.Raise(this, new StringEventArgs(m_SerialNumber));
			}
		}

		public void SetParent(ITelemetryProvider provider)
		{
			if (!(provider is BiampTesiraDevice))
				throw new InvalidOperationException(
					string.Format("Cannot create external telemetry for provider {0}, " +
								  "Provider must be of type BiampTesiraDevice.", provider));

			UnsubscribeParent(m_Parent);

			m_Parent = (BiampTesiraDevice)provider;

			SubscribeParent(m_Parent);

			Update(m_Parent);
		}

		private void Update(BiampTesiraDevice parent)
		{
			if (parent == null)
				return;

			FirmwareVersion = GetDeviceService(parent).FirmwareVersion;
			IpAddress = GetDeviceService(parent).IpAddress;
			Hostname = GetDeviceService(parent).Hostname;
			MacAddress = GetDeviceService(parent).MacAddress;
			SerialNumber = GetDeviceService(parent).SerialNumber;
			IpSubnet = GetDeviceService(parent).SubnetMask;
			IpGateway = GetDeviceService(parent).DefaultGateway;

			// Active Faults
			string faults = GetDeviceService(parent).ActiveFaultStatus;
			if (string.IsNullOrEmpty(faults) || string.Equals(faults, NO_FAULTS_MESSAGE, StringComparison.OrdinalIgnoreCase))
			{
				ActiveFaultState = false;
				ActiveFaultMessages = string.Empty;
				return;
			}
			ActiveFaultState = true;
			ActiveFaultMessages = faults;

		}

		private void SubscribeParent(BiampTesiraDevice parent)
		{
			if (parent == null)
				return;

			GetDeviceService(parent).OnFirmwareVersionChanged += ParentOnFirmwareVersionChanged;
			GetDeviceService(parent).OnFaultStatusChanged += ParentOnFaultStatusChanged;
			GetDeviceService(parent).OnIpAddressChanged += ParentOnIpAddressChanged;
			GetDeviceService(parent).OnHostnameChanged += ParentOnHostnameChanged;
			GetDeviceService(parent).OnMacAddressChanged += ParentOnMacAddressChanged;
			GetDeviceService(parent).OnSerialNumberChanged += ParentOnSerialNumberChanged;
			GetDeviceService(parent).OnSubnetMaskChanged += ParentOnSubnetMaskChanged;
			GetDeviceService(parent).OnDefaultGatewayChanged += ParentOnDefaultGatewayChanged;
		}

		private void UnsubscribeParent(BiampTesiraDevice parent)
		{
			if (m_Parent == null)
				return;

			GetDeviceService(parent).OnFirmwareVersionChanged -= ParentOnFirmwareVersionChanged;
			GetDeviceService(parent).OnFaultStatusChanged -= ParentOnFaultStatusChanged;
			GetDeviceService(parent).OnIpAddressChanged -= ParentOnIpAddressChanged;
			GetDeviceService(parent).OnHostnameChanged -= ParentOnHostnameChanged;
			GetDeviceService(parent).OnMacAddressChanged -= ParentOnMacAddressChanged;
			GetDeviceService(parent).OnSerialNumberChanged -= ParentOnSerialNumberChanged;
			GetDeviceService(parent).OnSubnetMaskChanged -= ParentOnSubnetMaskChanged;
			GetDeviceService(parent).OnDefaultGatewayChanged -= ParentOnDefaultGatewayChanged;
		}

		private void ParentOnIpAddressChanged(object sender, StringEventArgs e)
		{
			IpAddress = e.Data;
		}

		private void ParentOnSubnetMaskChanged(object sender, StringEventArgs e)
		{
			IpSubnet = e.Data;
		}

		private void ParentOnDefaultGatewayChanged(object sender, StringEventArgs e)
		{
			IpGateway = e.Data;
		}

		private DeviceService GetDeviceService(BiampTesiraDevice parent)
		{
			return parent.AttributeInterfaces.LazyLoadService<DeviceService>();
		}

		private void ParentOnFaultStatusChanged(object sender, StringEventArgs args)
		{
			string faults = args.Data;

			if (string.IsNullOrEmpty(faults) || string.Equals(faults, NO_FAULTS_MESSAGE, StringComparison.OrdinalIgnoreCase))
			{
				ActiveFaultState = false;
				ActiveFaultMessages = string.Empty;
				return;
			}
			ActiveFaultState = true;
			ActiveFaultMessages = faults;
		}

		private void ParentOnFirmwareVersionChanged(object sender, StringEventArgs args)
		{
			FirmwareVersion = args.Data;
		}

		private void ParentOnHostnameChanged (object sender, StringEventArgs e)
		{
			Hostname = e.Data;
		}

		private void ParentOnMacAddressChanged(object sender, StringEventArgs e)
		{
			MacAddress = e.Data;
		}

		private void ParentOnSerialNumberChanged(object sender, StringEventArgs e)
		{
			SerialNumber = e.Data;
		}
	}

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

		[DynamicPropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_STATE, DspTelemetryNames.ACTIVE_FAULT_STATE_CHANGED)]
		bool ActiveFaultState { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION, DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION_CHANGED)]
		string FirmwareVersion { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_ADDRESS, DeviceTelemetryNames.DEVICE_IP_ADDRESS_CHANGED)]
		string IpAddress { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_SUBNET, DeviceTelemetryNames.DEVICE_IP_SUBNET_CHANGED)]
		string IpSubnet { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_GATEWAY, DeviceTelemetryNames.DEVICE_IP_GATEWAY_CHANGED)]
		string IpGateway { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_HOSTNAME, DeviceTelemetryNames.DEVICE_HOSTNAME_CHANGED)]
		string Hostname { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_MAC_ADDRESS, DeviceTelemetryNames.DEVICE_MAC_ADDRESS_CHANGED)]
		string MacAddress { get; }

		[DynamicPropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_MESSAGE, DspTelemetryNames.ACTIVE_FAULT_MESSAGE_CHANGED)]
		string ActiveFaultMessages { get; }

		[DynamicPropertyTelemetry(DeviceTelemetryNames.DEVICE_SERIAL_NUMBER, DeviceTelemetryNames.DEVICE_SERIAL_NUMBER_CHANGED)]
		string SerialNumber { get; }
	}
}