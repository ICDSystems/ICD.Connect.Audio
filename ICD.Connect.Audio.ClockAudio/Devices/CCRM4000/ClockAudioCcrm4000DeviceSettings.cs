using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.ClockAudio.Devices.CCRM4000
{
	[KrangSettings("ClockAudioCcrm4000", typeof(ClockAudioCcrm4000Device))]
	public sealed class ClockAudioCcrm4000DeviceSettings : AbstractDeviceSettings
	{
		private const string EXTEND_RELAY_ID_ELEMENT = "ExtendRelay";
		private const string RETRACT_RELAY_ID_ELEMENT = "RetractRelay";
		private const string RELAY_LATCH_ELEMENT = "LatchRelay";
		private const string RELAY_HOLD_TIME_ELEMENT = "RelayHoldTime";

		private const bool RELAY_LATCH_DEFAULT = false;
		private const int RELAY_HOLD_TIME_DEFAULT = 500;

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? ExtendRelay { get; set; }

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? RetractRelay { get; set; }

		public bool RelayLatch { get; set; }

		public long RelayHoldTime { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(RETRACT_RELAY_ID_ELEMENT, IcdXmlConvert.ToString(RetractRelay));
			writer.WriteElementString(EXTEND_RELAY_ID_ELEMENT, IcdXmlConvert.ToString(ExtendRelay));
			writer.WriteElementString(RELAY_LATCH_ELEMENT, IcdXmlConvert.ToString(RelayLatch));
			writer.WriteElementString(RELAY_HOLD_TIME_ELEMENT, IcdXmlConvert.ToString(RelayHoldTime));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			RetractRelay = XmlUtils.TryReadChildElementContentAsInt(xml, RETRACT_RELAY_ID_ELEMENT);
			ExtendRelay = XmlUtils.TryReadChildElementContentAsInt(xml, EXTEND_RELAY_ID_ELEMENT);
			RelayLatch = XmlUtils.TryReadChildElementContentAsBoolean(xml, RELAY_LATCH_ELEMENT) ?? RELAY_LATCH_DEFAULT;
			RelayHoldTime = XmlUtils.TryReadChildElementContentAsInt(xml, RELAY_HOLD_TIME_ELEMENT) ?? RELAY_HOLD_TIME_DEFAULT;
		}
	}
}
