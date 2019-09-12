using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.ClockAudio.Devices.TS001
{
	[KrangSettings("ClockAudioTs001", typeof(ClockAudioTs001Device))]
	public sealed class ClockAudioTs001DeviceSettings : AbstractDeviceSettings
	{
		private const string BUTTON_INPUT_PORT_ELEMENT = "ButtonInputPort";
		private const string RED_LED_OUTPUT_PORT_ELEMENT = "RedLedOutputPort";
		private const string GREEN_LED_OUTPUT_PORT_ELEMENT = "GreenLedOutputPort";
		private const string VOLTAGE_INPUT_PORT_ELEMENT = "VoltageInputPort";

		[OriginatorIdSettingsProperty(typeof(IDigitalInputPort))]
		public int? ButtonInputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IIoPort))]
		public int? RedLedOutputPort { get; set; }

		[OriginatorIdSettingsProperty(typeof(IIoPort))]
		public int? GreenLedOutputPort { get; set; }

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
			writer.WriteElementString(RED_LED_OUTPUT_PORT_ELEMENT, IcdXmlConvert.ToString(RedLedOutputPort));
			writer.WriteElementString(GREEN_LED_OUTPUT_PORT_ELEMENT, IcdXmlConvert.ToString(GreenLedOutputPort));
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
			RedLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, RED_LED_OUTPUT_PORT_ELEMENT);
			GreenLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, GREEN_LED_OUTPUT_PORT_ELEMENT);
			VoltageInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, VOLTAGE_INPUT_PORT_ELEMENT);
		}
	}
}
