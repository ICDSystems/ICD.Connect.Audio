using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.QSys.Devices
{
	public abstract class AbstractNamedComponentQSysDeviceSettings : AbstractDeviceSettings, INamedComponentQSysDeviceSettings
	{
		private const string DSP_ID_ELEMENT = "Dsp";
		private const string COMPONENT_NAME_ELEMENT = "ComponentName";

		[OriginatorIdSettingsProperty(typeof(QSysCoreDevice))]
		public int DspId { get; set; }

		public string ComponentName { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DSP_ID_ELEMENT, IcdXmlConvert.ToString(DspId));
			writer.WriteElementString(COMPONENT_NAME_ELEMENT, ComponentName);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DspId = XmlUtils.ReadChildElementContentAsInt(xml, DSP_ID_ELEMENT);
			ComponentName = XmlUtils.TryReadChildElementContentAsString(xml, COMPONENT_NAME_ELEMENT);
		}
	}
}
