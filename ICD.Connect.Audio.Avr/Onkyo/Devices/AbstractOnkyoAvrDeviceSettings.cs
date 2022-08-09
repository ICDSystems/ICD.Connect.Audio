using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Avr.Onkyo.Devices
{
	public abstract class AbstractOnkyoAvrDeviceSettings : AbstractAvrDeviceSettings, IOnkyoAvrDeviceSettings
    {
        private const string PORT_ELEMENT = "Port";
        private const string MAX_VOLUME_ELEMENT = "MaxVolume";
        private const string COMMUNICATIONS_TYPE_ELEMENT = "CommunicationsType";

        public const int DEFAULT_MAX_VOLUME = 80;

		private readonly NetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }
		
		public int MaxVolume { get; set; }
		
		public eCommunicationsType CommunicationsType { get; set; }

		#region Network

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort? NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void INetworkProperties.ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

		#endregion

		#region Com Spec

		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		public eComBaudRates? ComSpecBaudRate
		{
			get { return m_ComSpecProperties.ComSpecBaudRate; }
			set { m_ComSpecProperties.ComSpecBaudRate = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		public eComDataBits? ComSpecNumberOfDataBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfDataBits; }
			set { m_ComSpecProperties.ComSpecNumberOfDataBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		public eComParityType? ComSpecParityType
		{
			get { return m_ComSpecProperties.ComSpecParityType; }
			set { m_ComSpecProperties.ComSpecParityType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		public eComStopBits? ComSpecNumberOfStopBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfStopBits; }
			set { m_ComSpecProperties.ComSpecNumberOfStopBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		public eComProtocolType? ComSpecProtocolType
		{
			get { return m_ComSpecProperties.ComSpecProtocolType; }
			set { m_ComSpecProperties.ComSpecProtocolType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		public eComHardwareHandshakeType? ComSpecHardwareHandshake
		{
			get { return m_ComSpecProperties.ComSpecHardwareHandshake; }
			set { m_ComSpecProperties.ComSpecHardwareHandshake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		public eComSoftwareHandshakeType? ComSpecSoftwareHandshake
		{
			get { return m_ComSpecProperties.ComSpecSoftwareHandshake; }
			set { m_ComSpecProperties.ComSpecSoftwareHandshake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		public bool? ComSpecReportCtsChanges
		{
			get { return m_ComSpecProperties.ComSpecReportCtsChanges; }
			set { m_ComSpecProperties.ComSpecReportCtsChanges = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void IComSpecProperties.ClearComSpecProperties()
		{
			m_ComSpecProperties.ClearComSpecProperties();
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractOnkyoAvrDeviceSettings()
		{
			m_ComSpecProperties = new ComSpecProperties();
			m_NetworkProperties = new NetworkProperties();
			MaxVolume = DEFAULT_MAX_VOLUME;

			UpdateNetworkDefaults();
			UpdateComSpecDefaults();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(MAX_VOLUME_ELEMENT, IcdXmlConvert.ToString(MaxVolume));
			writer.WriteElementString(COMMUNICATIONS_TYPE_ELEMENT, IcdXmlConvert.ToString(CommunicationsType));

			m_NetworkProperties.WriteElements(writer);
			m_ComSpecProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			MaxVolume = XmlUtils.TryReadChildElementContentAsInt(xml, MAX_VOLUME_ELEMENT) ?? DEFAULT_MAX_VOLUME;
			CommunicationsType = XmlUtils.TryReadChildElementContentAsEnum<eCommunicationsType>(xml, COMMUNICATIONS_TYPE_ELEMENT, true) ??
			                 eCommunicationsType.Auto;

			m_NetworkProperties.ParseXml(xml);
			m_ComSpecProperties.ParseXml(xml);

			UpdateNetworkDefaults();
			UpdateComSpecDefaults();
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		private void UpdateNetworkDefaults()
		{
			m_NetworkProperties.ApplyDefaultValues(null, 60128);
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		private void UpdateComSpecDefaults()
		{
			m_ComSpecProperties.ApplyDefaultValues(eComBaudRates.BaudRate9600,
												   eComDataBits.DataBits8,
												   eComParityType.None,
												   eComStopBits.StopBits1,
												   eComProtocolType.Rs232,
												   eComHardwareHandshakeType.None,
												   eComSoftwareHandshakeType.None,
												   false);
		}
    }
}