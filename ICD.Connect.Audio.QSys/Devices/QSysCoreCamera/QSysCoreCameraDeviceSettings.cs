using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.QSys.Devices.QSysCoreCamera
{
	[KrangSettings("QSysCoreCamera", typeof(QSysCoreCameraDevice))]
	public sealed class QSysCoreCameraDeviceSettings : AbstractNamedComponentQSysDeviceSettings, ICameraDeviceSettings
	{ 
		
		private const string SNAPSHOTS_NAME_ELEMENT = "SnapshotsName";

		public string SnapshotsName { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(SNAPSHOTS_NAME_ELEMENT, SnapshotsName);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			SnapshotsName = XmlUtils.TryReadChildElementContentAsString(xml, SNAPSHOTS_NAME_ELEMENT);
		}
	}
}
