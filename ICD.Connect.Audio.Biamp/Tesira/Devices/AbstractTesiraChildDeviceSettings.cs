using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public abstract class AbstractTesiraChildDeviceSettings : AbstractDeviceSettings, ITesiraChildDeviceSettings
	{
		private const string BIAMP_ID_ELEMENT = "Biamp";

		public int? BiampId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(BIAMP_ID_ELEMENT, IcdXmlConvert.ToString(BiampId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			BiampId = XmlUtils.TryReadChildElementContentAsInt(xml, BIAMP_ID_ELEMENT);
		}
	}
}