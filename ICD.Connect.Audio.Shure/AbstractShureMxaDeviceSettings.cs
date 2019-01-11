using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.Shure
{
	public abstract class AbstractShureMxaDeviceSettings : AbstractDeviceSettings, ISecureNetworkSettings
	{
		private const string PORT_ELEMENT = "Port";

		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername { get { return m_NetworkProperties.NetworkUsername; } set { m_NetworkProperties.NetworkUsername = value; } }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword { get { return m_NetworkProperties.NetworkPassword; } set { m_NetworkProperties.NetworkPassword = value; } }

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort? NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void INetworkProperties.ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractShureMxaDeviceSettings()
		{
			m_NetworkProperties = new SecureNetworkProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));

			m_NetworkProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);

			m_NetworkProperties.ParseXml(xml);
		}
	}
}
