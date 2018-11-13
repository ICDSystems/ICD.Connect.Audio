using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Biamp
{
	[KrangSettings("BiampTesira", typeof(BiampTesiraDevice))]
	public sealed class BiampTesiraDeviceSettings : AbstractDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string CONFIG_ELEMENT = "Config";

		private const string DEFAULT_USERNAME = "default";
		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private string m_UserName;
		private string m_ConfigPath;

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

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
		}
	}
}
