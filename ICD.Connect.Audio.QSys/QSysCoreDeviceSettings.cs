using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.QSys
{
	[KrangSettings("QSysCore", typeof(QSysCoreDevice))]
	public sealed class QSysCoreDeviceSettings : AbstractDeviceSettings, INetworkProperties
	{
		private const string PORT_ELEMENT = "Port";
		private const string CONFIG_ELEMENT = "Config";

		private const string DEFAULT_USERNAME = "";
		private const string DEFAULT_PASSWORD = "";
		private const ushort DEFAULT_NETWORK_PORT = 1710;
		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private readonly NetworkProperties m_NetworkProperties;

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

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string Username { get { return m_NetworkProperties.Username; } set { m_NetworkProperties.Username = value; } }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get { return m_NetworkProperties.Password; } set { m_NetworkProperties.Password = value; } }

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
		public ushort NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public QSysCoreDeviceSettings()
		{
			m_NetworkProperties = new NetworkProperties
			{
				Username = DEFAULT_USERNAME,
				Password = DEFAULT_PASSWORD,
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
			Config = XmlUtils.TryReadChildElementContentAsString(xml, CONFIG_ELEMENT);

			m_NetworkProperties.ParseXml(xml);
		}
	}
}
