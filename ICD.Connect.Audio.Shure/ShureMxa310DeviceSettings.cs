using System;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Shure
{
	[KrangSettings(FACTORY_NAME)]
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
	}
}