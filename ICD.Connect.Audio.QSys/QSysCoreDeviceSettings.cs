using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.QSys
{
	[KrangSettings("QSysCore", typeof(QSysCoreDevice))]
	public sealed class QSysCoreDeviceSettings : AbstractDeviceSettings, ISecureNetworkSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string CONFIG_ELEMENT = "Config";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";

		private const string DEFAULT_USERNAME = "";
		private const string DEFAULT_PASSWORD = "";
		private const ushort DEFAULT_NETWORK_PORT = 1710;
		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private readonly SecureNetworkProperties m_NetworkProperties;

		private string m_UserName;
		private string m_Password;
		private string m_ConfigPath;

		#region Properties

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		[PathSettingsProperty("QSys", ".xml")]
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

		public string Password
		{
			get
			{
				if (string.IsNullOrEmpty(m_Password))
					m_Password = DEFAULT_PASSWORD;
				return m_Password;
			}
			set { m_Password = value; }
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

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreDeviceSettings()
		{
			m_NetworkProperties = new SecureNetworkProperties
			{
				NetworkUsername = DEFAULT_USERNAME,
				NetworkPassword = DEFAULT_PASSWORD,
				NetworkPort = DEFAULT_NETWORK_PORT
			};
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
			writer.WriteElementString(PASSWORD_ELEMENT, Password);
			writer.WriteElementString(CONFIG_ELEMENT, Config);

			m_NetworkProperties.WriteElements(writer);
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
			Password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			Config = XmlUtils.TryReadChildElementContentAsString(xml, CONFIG_ELEMENT);

			m_NetworkProperties.ParseXml(xml);

			NetworkPort = NetworkPort ?? DEFAULT_NETWORK_PORT;
		}
	}
}
