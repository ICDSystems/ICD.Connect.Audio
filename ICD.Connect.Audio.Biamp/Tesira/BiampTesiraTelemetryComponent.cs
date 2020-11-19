using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.Services;

namespace ICD.Connect.Audio.Biamp.Tesira
{
	internal sealed class BiampTesiraTelemetryComponent
	{
		private const string NO_FAULTS_MESSAGE = "No fault in device";

		[NotNull]
		private readonly BiampTesiraDevice m_Tesira;

		[NotNull]
		private BiampTesiraDevice Tesira { get { return m_Tesira; } }

		internal BiampTesiraTelemetryComponent([NotNull] BiampTesiraDevice tesira)
		{
			if (tesira == null)
				throw new ArgumentNullException("tesira");
			m_Tesira = tesira;

			Subscribe(Tesira);
			Update();
		}

		//todo: Make these activites on the device
		private string ActiveFaultMessages { get; set; }
		private bool ActiveFaultState { get; set; }

		#region Methods

		private void Update()
		{
			DeviceService service = GetDeviceService(Tesira);
			if (service == null)
				return;

			Tesira.MonitoredDeviceInfo.FirmwareVersion = service.FirmwareVersion;
			Tesira.MonitoredDeviceInfo.SerialNumber = service.SerialNumber;
			Tesira.MonitoredDeviceInfo.NetworkInfo.Hostname = service.Hostname;
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Address = service.IpAddress;
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).MacAddress = service.MacAddress;
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4SubnetMask = service.SubnetMask;
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Gateway = service.DefaultGateway;

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

		private void Subscribe(BiampTesiraDevice parent)
		{

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

		private void ParentOnIpAddressChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Address = e.Data;
		}

		private void ParentOnSubnetMaskChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4SubnetMask = e.Data;
		}

		private void ParentOnDefaultGatewayChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Gateway = e.Data;
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
			Tesira.MonitoredDeviceInfo.FirmwareVersion = args.Data;
		}

		private void ParentOnHostnameChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.NetworkInfo.Hostname = e.Data;
		}

		private void ParentOnMacAddressChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).MacAddress = e.Data;
		}

		private void ParentOnSerialNumberChanged(object sender, StringEventArgs e)
		{
			Tesira.MonitoredDeviceInfo.SerialNumber = e.Data;
		}

		#endregion
	}
}