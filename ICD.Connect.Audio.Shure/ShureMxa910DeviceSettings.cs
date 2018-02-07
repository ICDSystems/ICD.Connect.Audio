using System;
using ICD.Common.Properties;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Shure
{
	public sealed class ShureMxa910DeviceSettings : AbstractShureMxaDeviceSettings
	{
		private const string FACTORY_NAME = "ShureMxa910";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(ShureMxa910Device); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static ShureMxa910DeviceSettings FromXml(string xml)
		{
			ShureMxa910DeviceSettings output = new ShureMxa910DeviceSettings();
			output.ParseXml(xml);
			return output;
		}
	}
}