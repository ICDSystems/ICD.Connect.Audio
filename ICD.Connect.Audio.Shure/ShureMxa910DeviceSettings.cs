using ICD.Common.Properties;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			ShureMxa910Device output = new ShureMxa910Device();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static ShureMxa910DeviceSettings FromXml(string xml)
		{
			ShureMxa910DeviceSettings output = new ShureMxa910DeviceSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}