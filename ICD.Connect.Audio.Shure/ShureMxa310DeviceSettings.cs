using System;
using ICD.Common.Properties;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Shure
{
	public sealed class ShureMxa310DeviceSettings : AbstractShureMxaDeviceSettings
	{
		private const string FACTORY_NAME = "ShureMxa310";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(ShureMxa310Device); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static ShureMxa310DeviceSettings FromXml(string xml)
		{
			ShureMxa310DeviceSettings output = new ShureMxa310DeviceSettings();
			output.ParseXml(xml);
			return output;
		}
	}
}