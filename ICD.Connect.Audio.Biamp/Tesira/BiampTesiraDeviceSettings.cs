﻿using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Biamp.Tesira
{
	[KrangSettings("BiampTesira", typeof(BiampTesiraDevice))]
	public sealed class BiampTesiraDeviceSettings : AbstractDeviceSettings, ISecureNetworkSettings, IComSpecSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string CONFIG_ELEMENT = "Config";

		private const ushort DEFAULT_NETWORK_PORT = 23;
		private const string DEFAULT_USERNAME = "default";

		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;

		private string m_UserName;
		private string m_ConfigPath;

		#region Properties

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		[PathSettingsProperty("Tesira", ".xml")]
		public string Config
		{
			get
			{
				if (string.IsNullOrEmpty(m_ConfigPath))
					m_ConfigPath = DEFAULT_CONFIG_PATH;
				return m_ConfigPath;
			}
			set { m_ConfigPath = value; }
		}

		public string Username
		{
			get
			{
				if (string.IsNullOrEmpty(m_UserName))
					m_UserName = DEFAULT_USERNAME;
				return m_UserName;
			}
			set { m_UserName = value; }
		}

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername { get { return m_NetworkProperties.NetworkUsername; } set { m_NetworkProperties.NetworkUsername = value; } }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword { get { return m_NetworkProperties.NetworkPassword; } set { m_NetworkProperties.NetworkPassword = value; } }

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
		public BiampTesiraDeviceSettings()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

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
			writer.WriteElementString(USERNAME_ELEMENT, Username);
			writer.WriteElementString(CONFIG_ELEMENT, Config);

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
			Username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			Config = XmlUtils.TryReadChildElementContentAsString(xml, CONFIG_ELEMENT);

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
			m_NetworkProperties.ApplyDefaultValues(null, DEFAULT_NETWORK_PORT, DEFAULT_USERNAME, null);
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		private void UpdateComSpecDefaults()
		{
			m_ComSpecProperties.ApplyDefaultValues(eComBaudRates.BaudRate115200,
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
