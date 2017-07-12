using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.Services
{
	public sealed class DeviceService : AbstractService
	{
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

		private string m_Hostname;
		private bool m_MdnsEnabled;
		private string m_SerialNumber;
		private bool m_TelnetDisabled;

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

		#region Properties

		[PublicAPI]
		public string Hostname
		{
			get { return m_Hostname; }
			private set
			{
				if (value == m_Hostname)
					return;

				m_Hostname = value;

				OnHostnameChanged.Raise(this, new StringEventArgs(m_Hostname));
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

				OnTelnetDisabledChanged.Raise(this, new BoolEventArgs(m_TelnetDisabled));
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
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			// Unsubscribe
			RequestAttribute(KnownRedundantDeviceStatesFeedback, AttributeCode.eCommand.Unsubscribe, KNOWN_REDUNDANT_DEVICE_STATES_ATTRIBUTE, null);
		}

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

			// Subscribe
			RequestAttribute(KnownRedundantDeviceStatesFeedback, AttributeCode.eCommand.Subscribe, KNOWN_REDUNDANT_DEVICE_STATES_ATTRIBUTE, null);
		}

		public void ManualFailover(int unitNumber)
		{
			RequestService(MANUAL_FAILOVER_SERVICE, new Value(unitNumber));
		}

		/// <summary>
		/// Reboots the device.
		/// </summary>
		public void Reboot()
		{
			RequestService(REBOOT_SEVICE, null);
		}

		/// <summary>
		/// Resets the device.
		/// </summary>
		public void Reset()
		{
			RequestService(RESET_SERVICE, null);
		}

		/// <summary>
		/// Recall the preset with the given id.
		/// </summary>
		/// <param name="id"></param>
		public void RecallPreset(int id)
		{
			RequestService(RECALL_PRESET_SERVICE, new Value(id));
		}

		/// <summary>
		/// Recall the preset with the given id and show failures.
		/// </summary>
		/// <param name="id"></param>
		public void RecallPresetShowFailures(int id)
		{
			RequestService(RECALL_PRESET_AND_SHOW_FAILURES_SERVICE, new Value(id));
		}

		/// <summary>
		/// Recall the preset with the given name.
		/// </summary>
		/// <param name="name"></param>
		public void RecallPresetByName(string name)
		{
			RequestService(RECALL_PRESET_BY_NAME_SERVICE, new Value(name));
		}

		/// <summary>
		/// Saves the preset to the given id.
		/// </summary>
		/// <param name="id"></param>
		public void SavePreset(int id)
		{
			RequestService(SAVE_PRESET_SERVICE, new Value(id));
		}

		/// <summary>
		/// Save the preset to the given name.
		/// </summary>
		/// <param name="name"></param>
		public void SavePresetByName(string name)
		{
			RequestService(SAVE_PRESET_BY_NAME_SERVICE, new Value(name));
		}

		/// <summary>
		/// Starts system audio.
		/// </summary>
		public void StartAudio()
		{
			RequestService(START_AUDIO_SERVICE, null);
		}

		/// <summary>
		/// Stops system audio.
		/// </summary>
		public void StopAudio()
		{
			RequestService(STOP_AUDIO_SERVICE, null);
		}

		/// <summary>
		/// Starts audio for the partition with the given id.
		/// </summary>
		/// <param name="partitionId"></param>
		public void StartPartitionAudio(int partitionId)
		{
			RequestService(START_PARTITION_AUDIO_SERVICE, new Value(partitionId));
		}

		/// <summary>
		/// Stops audio for the partition with the given id.
		/// </summary>
		/// <param name="partitionId"></param>
		public void StopPartitionAudio(int partitionId)
		{
			RequestService(STOP_PARTITION_AUDIO_SERVICE, new Value(partitionId));
		}

		#endregion

		#region Private Methods

		private void ActiveFaultsFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue activeFaults = value["value"] as ArrayValue;
			// todo
		}

		private void DiscoveredServersFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue discoveredServers = value["value"] as ArrayValue;
			// todo
		}

		private void DnsConfigFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue dnsConfig = value["value"] as ControlValue;
			// todo
		}

		private void DnsStatusFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue dnsStatus = value["value"] as ControlValue;
			// todo
		}

		private void HostnameFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Hostname = (value["value"] as Value).StringValue;
		}

		private void ResolverHostsTableFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue hostTable = value["value"] as ControlValue;
			// todo
		}

		private void MdnsEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MdnsEnabled = (value["value"] as Value).BoolValue;
		}

		private void NetworkStatusFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue networkStatus = value["value"] as ControlValue;
			// todo
		}

		private void SerialNumberFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			SerialNumber = (value["value"] as Value).StringValue;
		}

		private void TelnetDisabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			TelnetDisabled = (value["disabled"] as Value).BoolValue;
		}

		private void FirmwareVersionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Version = (value["value"] as Value).StringValue;
		}

		private void KnownRedundantDeviceStatesFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue deviceStates = value["deviceStates"] as ArrayValue;
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
