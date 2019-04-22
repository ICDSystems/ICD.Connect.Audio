using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.QSys.Devices.QSysCoreCamera
{
	[KrangSettings("QSysCoreCamera", typeof(QSysCoreCameraDevice))]
	public sealed class QSysCoreCameraDeviceSettings : AbstractCameraDeviceSettings
	{
		private const string DSP_ID_ELEMENT = "Dsp";
		private const string COMPONENT_NAME_ELEMENT = "ComponentName";
		private const string SNAPSHOTS_NAME_ELEMENT = "SnapshotsName";

		[OriginatorIdSettingsProperty(typeof(QSysCoreDevice))]
		public int? DspId { get; set; }

		public string ComponentName { get; set; }
		public string SnapshotsName { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DSP_ID_ELEMENT, IcdXmlConvert.ToString(DspId));
			writer.WriteElementString(COMPONENT_NAME_ELEMENT, ComponentName);
			writer.WriteElementString(SNAPSHOTS_NAME_ELEMENT, SnapshotsName);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DspId = XmlUtils.TryReadChildElementContentAsInt(xml, DSP_ID_ELEMENT);
			ComponentName = XmlUtils.TryReadChildElementContentAsString(xml, COMPONENT_NAME_ELEMENT);
			SnapshotsName = XmlUtils.TryReadChildElementContentAsString(xml, SNAPSHOTS_NAME_ELEMENT);
		}
	}
}
