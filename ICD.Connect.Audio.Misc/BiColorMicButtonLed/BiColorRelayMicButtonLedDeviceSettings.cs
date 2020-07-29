using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Misc.BiColorMicButtonLed
{
	[KrangSettings("BiColorRelayMicButton", typeof(BiColorRelayMicButtonLedDevice))]
	public sealed class BiColorRelayMicButtonLedDeviceSettings : AbstractBiColorMicButtonLedDeviceSettings
	{
		private const string POWER_OUTPUT_PORT_ELEMENT = "PowerOutputPort";
		private const string RED_LED_OUTPUT_PORT_ELEMENT = "RedLedOutputPort";
		private const string GREEN_LED_OUTPUT_PORT_ELEMENT = "GreenLedOutputPort";

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? PowerOutputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? RedLedOutputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? GreenLedOutputPort { get; set; }


		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(POWER_OUTPUT_PORT_ELEMENT, IcdXmlConvert.ToString(PowerOutputPort));
			writer.WriteElementString(RED_LED_OUTPUT_PORT_ELEMENT, IcdXmlConvert.ToString(RedLedOutputPort));
			writer.WriteElementString(GREEN_LED_OUTPUT_PORT_ELEMENT, IcdXmlConvert.ToString(GreenLedOutputPort));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			PowerOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, POWER_OUTPUT_PORT_ELEMENT);
			RedLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, RED_LED_OUTPUT_PORT_ELEMENT);
			GreenLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, GREEN_LED_OUTPUT_PORT_ELEMENT);
		}
	}
}