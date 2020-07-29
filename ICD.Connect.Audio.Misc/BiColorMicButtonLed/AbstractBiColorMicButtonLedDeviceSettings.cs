using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Misc.MicMuteButton;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Misc.BiColorMicButtonLed
{
	public abstract class AbstractBiColorMicButtonLedDeviceSettings : AbstractDeviceSettings, IMicMuteButtonDeviceSettings
	{

		private const string BUTTON_INPUT_PORT_ELEMENT = "ButtonInputPort";
		private const string VOLTAGE_INPUT_PORT_ELEMENT = "VoltageInputPort";

		[OriginatorIdSettingsProperty(typeof(IDigitalInputPort))]
		public int? ButtonInputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IIoPort))]
		public int? VoltageInputPort { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(BUTTON_INPUT_PORT_ELEMENT, IcdXmlConvert.ToString(ButtonInputPort));
			writer.WriteElementString(VOLTAGE_INPUT_PORT_ELEMENT, IcdXmlConvert.ToString(VoltageInputPort));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			ButtonInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, BUTTON_INPUT_PORT_ELEMENT);
			VoltageInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, VOLTAGE_INPUT_PORT_ELEMENT);
		}
	}
}