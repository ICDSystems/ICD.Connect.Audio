using ICD.Common.Properties;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			ShureMxa310Device output = new ShureMxa310Device();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static ShureMxa310DeviceSettings FromXml(string xml)
		{
			ShureMxa310DeviceSettings output = new ShureMxa310DeviceSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}