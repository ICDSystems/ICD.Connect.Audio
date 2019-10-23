using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Shure.Devices.MX
{
	[KrangSettings("ShureMx396", typeof(ShureMx396Device))]
	public sealed class ShureMx396DeviceSettings : AbstractDeviceSettings
	{
		private const string BUTTON_INPUT_PORT_ELEMENT = "ButtonInputPort";
		private const string LED_STATE_PORT_ELEMENT = "LedStatePort";

		[OriginatorIdSettingsProperty(typeof(IDigitalInputPort))]
		public int? ButtonInputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? LedStatePort { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(BUTTON_INPUT_PORT_ELEMENT, IcdXmlConvert.ToString(ButtonInputPort));
			writer.WriteElementString(LED_STATE_PORT_ELEMENT, IcdXmlConvert.ToString(LedStatePort));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			ButtonInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, BUTTON_INPUT_PORT_ELEMENT);
			LedStatePort = XmlUtils.TryReadChildElementContentAsInt(xml, LED_STATE_PORT_ELEMENT);
		}
	}
}
