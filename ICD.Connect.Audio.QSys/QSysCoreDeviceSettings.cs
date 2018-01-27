using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.QSys
{
	public sealed class QSysCoreDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "QSysCore";

		private const string PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";
		private const string CONFIG_ELEMENT = "Config";

		private const string DEFAULT_USERNAME = "";
		private const string DEFAULT_PASSWORD = "";
		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private string m_UserName;
		private string m_Password;
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

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(QSysCoreDevice); } }

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
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static QSysCoreDeviceSettings FromXml(string xml)
		{
			int? port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			string username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			string password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			string config = XmlUtils.TryReadChildElementContentAsString(xml, CONFIG_ELEMENT);

			QSysCoreDeviceSettings output = new QSysCoreDeviceSettings
			{
				Port = port,
				Username = username,
				Password = password
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
