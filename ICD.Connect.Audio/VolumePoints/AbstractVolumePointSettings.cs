using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.VolumePoints
{
	[PublicAPI]
	public abstract class AbstractVolumePointSettings : AbstractSettings, IVolumePointSettings
	{
		private const string DEVICE_ELEMENT = "Device";
		private const string CONTROL_ELEMENT = "Control";

		#region Properties

		/// <summary>
		/// Device id for this volume point
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(IDeviceBase))]
		public int DeviceId { get; set; }

		/// <summary>
		/// Control id for an IVolumeControl on this volume point's device
		/// </summary>
		public int ControlId { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Write property elements to xml
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DEVICE_ELEMENT, IcdXmlConvert.ToString(DeviceId));
			writer.WriteElementString(CONTROL_ELEMENT, IcdXmlConvert.ToString(ControlId));
		}

		/// <summary>
		/// Instantiate volume point settings from an xml element
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DeviceId = XmlUtils.TryReadChildElementContentAsInt(xml, DEVICE_ELEMENT) ?? 0;
			ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, CONTROL_ELEMENT) ?? 0;
		}

		#endregion

		public override int DependencyCount { get { return DeviceId == 0 ? 0 : 1; } }

		/// <summary>
		/// Returns true if the settings depend on a device with the given ID.
		/// For example, to instantiate an IR Port from settings, the device the physical port
		/// belongs to will need to be instantiated first.
		/// </summary>
		/// <returns></returns>
		public override bool HasDeviceDependency(int id)
		{
			return id != 0 && id == DeviceId;
		}
	}
}
