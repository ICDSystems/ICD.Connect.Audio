using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Biamp
{
	public sealed class BiampTesiraDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "BiampTesira";

		private const string PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string CONFIG_ELEMENT = "Config";

		private const string DEFAULT_CONFIG_PATH = "ControlConfig.xml";

		private string m_ConfigPath;

		/// <summary>
		/// The port id.
		/// </summary>
		[SettingsProperty(SettingsProperty.ePropertyType.Id, typeof(ISerialPort))]
		public int? Port { get; set; }

		public string Username { get; set; }

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
		public override Type OriginatorType { get { return typeof(BiampTesiraDevice); } }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (Port != null)
				writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString((int)Port));

			if (!string.IsNullOrEmpty(Username))
				writer.WriteElementString(USERNAME_ELEMENT, Username);

			if (!string.IsNullOrEmpty(Config))
				writer.WriteElementString(CONFIG_ELEMENT, Config);
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static BiampTesiraDeviceSettings FromXml(string xml)
		{
			int? port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			string username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			string config = XmlUtils.TryReadChildElementContentAsString(xml, CONFIG_ELEMENT);

			BiampTesiraDeviceSettings output = new BiampTesiraDeviceSettings
			{
				Port = port,
				Username = username,
				Config = config
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
