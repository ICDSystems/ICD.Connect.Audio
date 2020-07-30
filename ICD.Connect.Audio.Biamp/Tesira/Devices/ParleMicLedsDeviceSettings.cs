using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	[KrangSettings("ParleMicLedsDevice", typeof(ParleMicLedsDevice))]
	public sealed class ParleMicLedsDeviceSettings : AbstractDeviceSettings
	{
		private const string INSTANCE_TAG_ELEMENT = "InstanceTag";
		private const string BIAMP_ID_ELEMENT = "Biamp";

		public string InstanceTag { get; set; }

		[OriginatorIdSettingsProperty(typeof(BiampTesiraDevice))]
		public int? BiampId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(INSTANCE_TAG_ELEMENT, InstanceTag);
			writer.WriteElementString(BIAMP_ID_ELEMENT, IcdXmlConvert.ToString(BiampId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			InstanceTag = XmlUtils.TryReadChildElementContentAsString(xml, INSTANCE_TAG_ELEMENT);
			BiampId = XmlUtils.TryReadChildElementContentAsInt(xml, BIAMP_ID_ELEMENT);
		}
	}
}