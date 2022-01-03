using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Devices.Microphones;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Shure.Devices
{
	public abstract class AbstractShureMicDeviceSettings : AbstractMicrophoneDeviceSettings, INetworkSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const ushort DEFAULT_TCP_PORT_NUMBER = 2202;

		private readonly NetworkProperties m_NetworkProperties;

		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		public NetworkProperties NetworkProperties {get { return m_NetworkProperties; }}

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return NetworkProperties.NetworkAddress; }
			set { NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort? NetworkPort
		{
			get { return NetworkProperties.NetworkPort; }
			set { NetworkProperties.NetworkPort = value; }
		}

		protected AbstractShureMicDeviceSettings()
		{
			m_NetworkProperties = new NetworkProperties();
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public void ClearNetworkProperties()
		{
			NetworkProperties.ClearNetworkProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			NetworkProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			NetworkProperties.ParseXml(xml);
			UpdateNetworkDefaults();
		}

		/// <summary>
		/// Updates the network properties with default values.
		/// </summary>
		private void UpdateNetworkDefaults()
		{
			NetworkProperties.ApplyDefaultValues(null, DEFAULT_TCP_PORT_NUMBER);
		}
	}
}
