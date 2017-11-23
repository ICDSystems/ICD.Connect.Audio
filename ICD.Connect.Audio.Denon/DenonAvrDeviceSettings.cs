using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Denon
{
	public sealed class DenonAvrDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "DenonAvr";

		private const string PORT_ELEMENT = "Port";

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(DenonAvrDevice); } }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static DenonAvrDeviceSettings FromXml(string xml)
		{
			int? port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);

			DenonAvrDeviceSettings output = new DenonAvrDeviceSettings
			{
				Port = port
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
