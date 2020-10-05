using ICD.Common.Utils.Xml;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public abstract class AbstractTesiraChildAttributeInterfaceDeviceSettings : AbstractTesiraChildDeviceSettings, ITesiraChildAttributInterfaceDeviceSettings
	{
		private const string INSTANCE_TAG_ELEMENT = "InstanceTag";

		public string InstanceTag { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(INSTANCE_TAG_ELEMENT, InstanceTag);
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			InstanceTag = XmlUtils.TryReadChildElementContentAsString(xml, INSTANCE_TAG_ELEMENT);
		}
	}
}