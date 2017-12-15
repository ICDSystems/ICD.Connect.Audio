using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.Services
{
	public sealed class DeviceService : AbstractService
	{
        public enum eLinkStatus
        {
            Link1Gb,
        }

        private static readonly Dictionary<string, eLinkStatus> s_LinkStatus
            = new Dictionary<string, eLinkStatus>(StringComparer.OrdinalIgnoreCase)
			{
				{"LINK_1_GB", eLinkStatus.Link1Gb},
			};


		public const string INSTANCE_TAG = "DEVICE";

		private const string MANUAL_FAILOVER_SERVICE = "manualFailover";
		private const string REBOOT_SEVICE = "reboot";
		private const string RESET_SERVICE = "deleteConfigData";
		private const string RECALL_PRESET_SERVICE = "recallPreset";
		private const string RECALL_PRESET_AND_SHOW_FAILURES_SERVICE = "recallPresetShowFailures";
		private const string RECALL_PRESET_BY_NAME_SERVICE = "recallPresetByName";
		private const string SAVE_PRESET_SERVICE = "savePreset";
		private const string SAVE_PRESET_BY_NAME_SERVICE = "savePresetByName";
		private const string START_AUDIO_SERVICE = "startAudio";
		private const string STOP_AUDIO_SERVICE = "stopAudio";
		private const string START_PARTITION_AUDIO_SERVICE = "startPartitionAudio";
		private const string STOP_PARTITION_AUDIO_SERVICE = "stopPartitionAudio";

		private const string ACTIVE_FAULTS_ATTRIBUTE = "activeFaultList";
		private const string DISCOVERED_SERVERS_ATTRIBUTE = "discoveredServers";
		private const string DNS_CONFIG_ATTRIBUTE = "dnsConfig";
		private const string DNS_STATUS_ATTRIBUTE = "dnsStatus";
		private const string HOSTNAME_ATTRIBUTE = "hostname";
		private const string RESOLVER_HOSTS_TABLE = "hostTable";
		private const string NETWORK_INTERFACE_CONFIG_ATTRIBUTE = "ipConfig";
		private const string NETWORK_INTERFACE_STATUS_ATTRIBUTE = "ipStatus";
		private const string KNOWN_REDUNDANT_DEVICE_STATES_ATTRIBUTE = "knownRedundantDeviceStates";
		private const string MDNS_ENABLED_ATTRIBUTE = "mDNSEnabled";
		private const string NETWORK_STATUS_ATTRIBUTE = "networkStatus";
		private const string SERIAL_NUMBER_ATTRIBUTE = "serialNumber";
		private const string TELNET_DISABLED_ATTRIBUTE = "telnetDisabled";
		private const string FIRMWARE_VERSION_ATTRIBUTE = "version";

	    private const string VOIP_CONTROL_STATUS = "protocols";

        public delegate void LinkStatusCallback(DeviceService sender, eLinkStatus status);

		private string m_Hostname;
		private bool m_MdnsEnabled;
		private string m_SerialNumber;
		private bool m_TelnetDisabled;
	    private string m_IpAddress;
	    private string m_FaultStatus;
	    private string m_DefaultGateway;
	    private eLinkStatus m_LinkStatus;
	    private string m_SubnetMask;
	    private string m_MacAddress;
	    private string m_Registration;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnHostnameChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMdnsEnabledChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSerialNumberChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnTelnetDisabledChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnVersionChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnFaultStatusChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnDefaultGatewayChanged;

        [PublicAPI]
	    public event LinkStatusCallback OnLinkStatusChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnIpAddressChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnSubnetMaskChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnMacAddressChanged;

	    [PublicAPI]
	    public event EventHandler<StringEventArgs> OnRegistrationChanged;

		#region Properties

	    [PublicAPI]
	    public string ActiveFaultStatus
	    {
	        get { return m_FaultStatus; }
	        private set
	        {
	            if (value == m_FaultStatus)
	                return;
	            m_FaultStatus = value;
                Log(eSeverity.Informational, "Active Fault Status set to {0}", m_FaultStatus);

	            OnFaultStatusChanged.Raise(this, new StringEventArgs(m_FaultStatus));
	        }
	    }

		[PublicAPI]
		public string Hostname
		{
			get { return m_Hostname; }
			private set
			{
				if (value == m_Hostname)
					return;

				m_Hostname = value;

				Log(eSeverity.Informational, "Hostname set to {0}", m_Hostname);

				OnHostnameChanged.Raise(this, new StringEventArgs(m_Hostname));
			}
		}

	    [PublicAPI]
	    public string DefaultGateway
	    {
            get { return m_DefaultGateway; }
	        private set
	        {
	            if (value == m_DefaultGateway)
	                return;
	            m_DefaultGateway = value;
	            Log(eSeverity.Informational, "Default Gateway set to {0}", m_DefaultGateway);
                OnDefaultGatewayChanged.Raise(this, new StringEventArgs(m_DefaultGateway));
	        }
	    }

	    [PublicAPI]
	    public eLinkStatus LinkStatus
	    {
	        get { return m_LinkStatus; }
	        private set
	        {
	            if (value == m_LinkStatus)
	                return;
	            m_LinkStatus = value;
                Log(eSeverity.Informational, "Link Status set to {0}", m_LinkStatus);

	            LinkStatusCallback handler = OnLinkStatusChanged;
                if (handler != null)
                    handler(this, m_LinkStatus);
	        }
	    }

	    [PublicAPI]
	    public string IpAddress 
        { 
            get { return m_IpAddress; }
	        private set
	        {
	            if (value == m_IpAddress)
	                return;
	            m_IpAddress = value;
	            Log(eSeverity.Informational, "IP Address set to {0}", m_IpAddress);
	            OnIpAddressChanged.Raise(this, new StringEventArgs(m_IpAddress));
	        }
	    }

	    [PublicAPI]
	    public string SubnetMask
	    {
	        get { return m_SubnetMask; }
	        private set
	        {
	            if (value == m_SubnetMask)
	                return;
	            m_SubnetMask = value;
                Log(eSeverity.Informational, "Subnet Mask set to {0}", m_SubnetMask);

	            OnSubnetMaskChanged.Raise(this, new StringEventArgs(m_SubnetMask));
	        }
	    }

		[PublicAPI]
		public bool MdnsEnabled
		{
			get { return m_MdnsEnabled; }
			private set
			{
				if (value == m_MdnsEnabled)
					return;

				m_MdnsEnabled = value;

				Log(eSeverity.Informational, "MdnsEnabled set to {0}", m_MdnsEnabled);

				OnMdnsEnabledChanged.Raise(this, new BoolEventArgs(m_MdnsEnabled));
			}
		}

		[PublicAPI]
		public string SerialNumber
		{
			get { return m_SerialNumber; }
			private set
			{
				if (value == m_SerialNumber)
					return;

				m_SerialNumber = value;

				Log(eSeverity.Informational, "SerialNumber set to {0}", m_SerialNumber);

				OnSerialNumberChanged.Raise(this, new StringEventArgs(m_SerialNumber));
			}
		}

		[PublicAPI]
		public bool TelnetDisabled
		{
			get { return m_TelnetDisabled; }
			private set
			{
				if (value == m_TelnetDisabled)
					return;

				m_TelnetDisabled = value;

				Log(eSeverity.Informational, "TelnetDisabled set to {0}", m_TelnetDisabled);

				OnTelnetDisabledChanged.Raise(this, new BoolEventArgs(m_TelnetDisabled));
			}
		}

	    [PublicAPI]
	    public string MacAddress
	    {
	        get { return m_MacAddress; }
	        private set
	        {
	            if (value == m_MacAddress)
	                return;
	            m_MacAddress = value;
                Log(eSeverity.Informational, "Mac Address set to {0}", m_MacAddress);
                OnMacAddressChanged.Raise(this, new StringEventArgs(m_MacAddress));
	        }
	    }

	    [PublicAPI]
	    public string Registration
	    {
	        get { return m_Registration; }
	        private set
	        {
	            if (value == m_Registration)
	                return;
	            m_Registration = value;
                Log(eSeverity.Informational, "Registration set to {0}", m_Registration);
                OnRegistrationChanged.Raise(this, new StringEventArgs(m_Registration));
	        }
	    }

		[PublicAPI]
		public string Version { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		public DeviceService(BiampTesiraDevice device)
			: base(device, INSTANCE_TAG)
		{
			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get intial values
			RequestAttribute(ActiveFaultsFeedback, AttributeCode.eCommand.Get, ACTIVE_FAULTS_ATTRIBUTE, null);
			RequestAttribute(DiscoveredServersFeedback, AttributeCode.eCommand.Get, DISCOVERED_SERVERS_ATTRIBUTE, null);
			RequestAttribute(DnsConfigFeedback, AttributeCode.eCommand.Get, DNS_CONFIG_ATTRIBUTE, null);
			RequestAttribute(DnsStatusFeedback, AttributeCode.eCommand.Get, DNS_STATUS_ATTRIBUTE, null);
			RequestAttribute(HostnameFeedback, AttributeCode.eCommand.Get, HOSTNAME_ATTRIBUTE, null);
			RequestAttribute(ResolverHostsTableFeedback, AttributeCode.eCommand.Get, RESOLVER_HOSTS_TABLE, null);
			RequestAttribute(KnownRedundantDeviceStatesFeedback, AttributeCode.eCommand.Get, KNOWN_REDUNDANT_DEVICE_STATES_ATTRIBUTE, null);
			RequestAttribute(MdnsEnabledFeedback, AttributeCode.eCommand.Get, MDNS_ENABLED_ATTRIBUTE, null);
			RequestAttribute(NetworkStatusFeedback, AttributeCode.eCommand.Get, NETWORK_STATUS_ATTRIBUTE, null);
			RequestAttribute(SerialNumberFeedback, AttributeCode.eCommand.Get, SERIAL_NUMBER_ATTRIBUTE, null);
			RequestAttribute(TelnetDisabledFeedback, AttributeCode.eCommand.Get, TELNET_DISABLED_ATTRIBUTE, null);
			RequestAttribute(FirmwareVersionFeedback, AttributeCode.eCommand.Get, FIRMWARE_VERSION_ATTRIBUTE, null);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			// Subscribe
			RequestAttribute(KnownRedundantDeviceStatesFeedback, command, KNOWN_REDUNDANT_DEVICE_STATES_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void ManualFailover(int unitNumber)
		{
			RequestService(MANUAL_FAILOVER_SERVICE, new Value(unitNumber));
		}

		/// <summary>
		/// Reboots the device.
		/// </summary>
		[PublicAPI]
		public void Reboot()
		{
			RequestService(REBOOT_SEVICE, null);
		}

		/// <summary>
		/// Resets the device.
		/// </summary>
		[PublicAPI]
		public void Reset()
		{
			RequestService(RESET_SERVICE, null);
		}

		/// <summary>
		/// Recall the preset with the given id.
		/// </summary>
		/// <param name="id"></param>
		[PublicAPI]
		public void RecallPreset(int id)
		{
			RequestService(RECALL_PRESET_SERVICE, new Value(id));
		}

		/// <summary>
		/// Recall the preset with the given id and show failures.
		/// </summary>
		/// <param name="id"></param>
		[PublicAPI]
		public void RecallPresetShowFailures(int id)
		{
			RequestService(RECALL_PRESET_AND_SHOW_FAILURES_SERVICE, new Value(id));
		}

		/// <summary>
		/// Recall the preset with the given name.
		/// </summary>
		/// <param name="name"></param>
		[PublicAPI]
		public void RecallPresetByName(string name)
		{
			RequestService(RECALL_PRESET_BY_NAME_SERVICE, new Value(name));
		}

		/// <summary>
		/// Saves the preset to the given id.
		/// </summary>
		/// <param name="id"></param>
		[PublicAPI]
		public void SavePreset(int id)
		{
			RequestService(SAVE_PRESET_SERVICE, new Value(id));
		}

		/// <summary>
		/// Save the preset to the given name.
		/// </summary>
		/// <param name="name"></param>
		[PublicAPI]
		public void SavePresetByName(string name)
		{
			RequestService(SAVE_PRESET_BY_NAME_SERVICE, new Value(name));
		}

		/// <summary>
		/// Starts system audio.
		/// </summary>
		[PublicAPI]
		public void StartAudio()
		{
			RequestService(START_AUDIO_SERVICE, null);
		}

		/// <summary>
		/// Stops system audio.
		/// </summary>
		[PublicAPI]
		public void StopAudio()
		{
			RequestService(STOP_AUDIO_SERVICE, null);
		}

		/// <summary>
		/// Starts audio for the partition with the given id.
		/// </summary>
		/// <param name="partitionId"></param>
		[PublicAPI]
		public void StartPartitionAudio(int partitionId)
		{
			RequestService(START_PARTITION_AUDIO_SERVICE, new Value(partitionId));
		}

		/// <summary>
		/// Stops audio for the partition with the given id.
		/// </summary>
		/// <param name="partitionId"></param>
		[PublicAPI]
		public void StopPartitionAudio(int partitionId)
		{
			RequestService(STOP_PARTITION_AUDIO_SERVICE, new Value(partitionId));
		}

		#endregion

		#region Private Methods

		private void ActiveFaultsFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue activeFaults = value.GetValue<ArrayValue>("value").FirstOrDefault() as ControlValue;
		    if (activeFaults == null)
		        return;
		    ActiveFaultStatus = activeFaults.GetValue<Value>("name").StringValue;
		}

		private void DiscoveredServersFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue discoveredServers = value.GetValue<ArrayValue>("value");
			// todo
		}

		private void DnsConfigFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue dnsConfig = value.GetValue<ControlValue>("value");
			// todo
		}

		private void DnsStatusFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue dnsStatus = value.GetValue<ControlValue>("value");
			// todo
		}

		private void HostnameFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Hostname = innerValue.StringValue;
		}

		private void ResolverHostsTableFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue hostTable = value.GetValue<ControlValue>("value");
			// todo
		}

		private void MdnsEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			MdnsEnabled = innerValue.BoolValue;
		}

		private void NetworkStatusFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue networkStatus = value.GetValue<ControlValue>("value");
		    Hostname = networkStatus.GetValue<Value>("hostname").StringValue;
		    DefaultGateway = networkStatus.GetValue<Value>("defaultGatewayStatus").StringValue;
		    ArrayValue networkInterfaceValueWithName = networkStatus.GetValue<ArrayValue>("networkInterfaceStatusWithName");
		    ControlValue networkInterface = networkInterfaceValueWithName.FirstOrDefault() as ControlValue;
            
            if (networkInterface == null)
		        return;

		    ControlValue networkInterfaceStatus = networkInterface.GetValue<ControlValue>("networkInterfaceStatus");

            LinkStatus = networkInterfaceStatus.GetValue<Value>("linkStatus").GetObjectValue(s_LinkStatus);
		    MacAddress = networkInterfaceStatus.GetValue<Value>("macAddress").StringValue;
		    IpAddress = networkInterfaceStatus.GetValue<Value>("ip").StringValue;
		    SubnetMask = networkInterfaceStatus.GetValue<Value>("netmask").StringValue;
		}

		private void SerialNumberFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			SerialNumber = innerValue.StringValue;
		}

		private void TelnetDisabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("disabled");
			TelnetDisabled = innerValue.BoolValue;
		}

		private void FirmwareVersionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Version = innerValue.StringValue;
		}

		private void KnownRedundantDeviceStatesFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue deviceStates = value.GetValue<ArrayValue>("deviceStates");
			// todo
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Hostname", Hostname);
		    addRow("IP Address", IpAddress);
		    addRow("Default Gateway", DefaultGateway);
		    addRow("Registration Status", Registration);
            addRow("Active Fault Status", ActiveFaultStatus);
		    addRow("Host Name", Hostname);
		    addRow("Link Status", LinkStatus);
		    addRow("Subnet Mask", SubnetMask);
		    addRow("Mac Address", MacAddress);
			addRow("MDNS Enabled", MdnsEnabled);
			addRow("Serial Number", SerialNumber);
			addRow("Telnet Disabled", TelnetDisabled);
			addRow("Version", Version);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("ManualFailover", "ManualFailover <UNIT NUMBER>", i => ManualFailover(i));

			yield return new ConsoleCommand("Reboot", "Reboots the Tesira device", () => Reboot());
			yield return new ConsoleCommand("Reset", "Resets the Tesira device", () => Reset());

			yield return new GenericConsoleCommand<int>("RecallPreset", "RecallPreset <ID>", i => RecallPreset(i));
			yield return
				new GenericConsoleCommand<int>("RecallPresetShowFailures", "RecallPresetShowFailures <ID>", i => RecallPresetShowFailures(i))
				;
			yield return new GenericConsoleCommand<string>("RecallPresetByName", "RecallPresetByName <NAME>", s => RecallPresetByName(s))
				;

			yield return new GenericConsoleCommand<int>("SavePreset", "SavePreset <ID>", i => SavePreset(i));
			yield return new GenericConsoleCommand<string>("SavePresetByName", "SavePresetByName <NAME>", s => SavePresetByName(s));

			yield return new ConsoleCommand("StartAudio", "Starts the system audio", () => StartAudio());
			yield return new ConsoleCommand("StopAudio", "Stops the system audio", () => StopAudio());
			yield return
				new GenericConsoleCommand<int>("StartPartitionAudio", "StartPartitionAudio <PARTITION ID>", i => StartPartitionAudio(i));
			yield return
				new GenericConsoleCommand<int>("StopPartitionAudio", "StopPartitionAudio <PARTITION ID>", i => StopPartitionAudio(i));
		}

		/// <summary>
		/// Workaround for unverifiable code warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
