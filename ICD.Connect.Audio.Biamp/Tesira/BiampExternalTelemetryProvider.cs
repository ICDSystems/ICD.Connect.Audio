using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.Services;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Audio.Biamp.Tesira
{
	public sealed class BiampExternalTelemetryProvider : AbstractExternalTelemetryProvider<BiampTesiraDevice>, IBiampExternalTelemetryProvider
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

		private bool m_ActiveFaultState;
		private string m_FirmwareVersion;
		private string m_IpAddress;
		private string m_Hostname;
		private string m_MacAddress;
		private string m_ActiveFaultMessages;
		private string m_SerialNumber;
		private string m_IpSubnet;
		private string m_IpGateway;

		#region Properties

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

		#endregion

		#region Methods

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(BiampTesiraDevice parent)
		{
			base.SetParent(parent);

			Update();
		}

		private void Update()
		{
			DeviceService service = Parent == null ? null : GetDeviceService(Parent);
			if (service == null)
				return;

			FirmwareVersion = service.FirmwareVersion;
			IpAddress = service.IpAddress;
			Hostname = service.Hostname;
			MacAddress = service.MacAddress;
			SerialNumber = service.SerialNumber;
			IpSubnet = service.SubnetMask;
			IpGateway = service.DefaultGateway;

			// Active Faults
			string faults = service.ActiveFaultStatus;
			if (string.IsNullOrEmpty(faults) || string.Equals(faults, NO_FAULTS_MESSAGE, StringComparison.OrdinalIgnoreCase))
			{
				ActiveFaultState = false;
				ActiveFaultMessages = string.Empty;
				return;
			}
			ActiveFaultState = true;
			ActiveFaultMessages = faults;

		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(BiampTesiraDevice parent)
		{
			base.Subscribe(parent);

			DeviceService service = parent == null ? null : GetDeviceService(parent);
			if (service == null)
				return;

			service.OnFirmwareVersionChanged += ParentOnFirmwareVersionChanged;
			service.OnFaultStatusChanged += ParentOnFaultStatusChanged;
			service.OnIpAddressChanged += ParentOnIpAddressChanged;
			service.OnHostnameChanged += ParentOnHostnameChanged;
			service.OnMacAddressChanged += ParentOnMacAddressChanged;
			service.OnSerialNumberChanged += ParentOnSerialNumberChanged;
			service.OnSubnetMaskChanged += ParentOnSubnetMaskChanged;
			service.OnDefaultGatewayChanged += ParentOnDefaultGatewayChanged;
		}

		protected override void Unsubscribe(BiampTesiraDevice parent)
		{
			base.Unsubscribe(parent);

			DeviceService service = parent == null ? null : GetDeviceService(parent);
			if (service == null)
				return;

			service.OnFirmwareVersionChanged -= ParentOnFirmwareVersionChanged;
			service.OnFaultStatusChanged -= ParentOnFaultStatusChanged;
			service.OnIpAddressChanged -= ParentOnIpAddressChanged;
			service.OnHostnameChanged -= ParentOnHostnameChanged;
			service.OnMacAddressChanged -= ParentOnMacAddressChanged;
			service.OnSerialNumberChanged -= ParentOnSerialNumberChanged;
			service.OnSubnetMaskChanged -= ParentOnSubnetMaskChanged;
			service.OnDefaultGatewayChanged -= ParentOnDefaultGatewayChanged;
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

		#endregion
	}
}