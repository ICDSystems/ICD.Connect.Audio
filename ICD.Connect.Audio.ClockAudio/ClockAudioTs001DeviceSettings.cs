using ICD.Common.Attributes.Properties;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.ClockAudio
{
	public sealed class ClockAudioTs001DeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ClockAudioTs001";

		private const string BUTTON_INPUT_PORT_ELEMENT = "ButtonInputPort";
		private const string RED_LED_OUTPUT_PORT_ELEMENT = "RedLedOutputPort";
		private const string GREEN_LED_OUTPUT_PORT_ELEMENT = "GreenLedOutputPort";
		private const string VOLTAGE_INPUT_PORT_ELEMENT = "VoltageInputPort";

		[SettingsProperty(SettingsProperty.ePropertyType.PortId)]
		public int? ButtonInputPort { get; set; }

		[SettingsProperty(SettingsProperty.ePropertyType.PortId)]
		public int? RedLedOutputPort { get; set; }

		[SettingsProperty(SettingsProperty.ePropertyType.PortId)]
		public int? GreenLedOutputPort { get; set; }

		[SettingsProperty(SettingsProperty.ePropertyType.PortId)]
		public int? VoltageInputPort { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			ClockAudioTs001Device output = new ClockAudioTs001Device();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static ClockAudioTs001DeviceSettings FromXml(string xml)
		{
			ClockAudioTs001DeviceSettings output = new ClockAudioTs001DeviceSettings
			{
				ButtonInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, BUTTON_INPUT_PORT_ELEMENT),
				RedLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, RED_LED_OUTPUT_PORT_ELEMENT),
				GreenLedOutputPort = XmlUtils.TryReadChildElementContentAsInt(xml, GREEN_LED_OUTPUT_PORT_ELEMENT),
				VoltageInputPort = XmlUtils.TryReadChildElementContentAsInt(xml, VOLTAGE_INPUT_PORT_ELEMENT)
			};

			ParseXml(output, xml);
			return output;
		}
	}
}