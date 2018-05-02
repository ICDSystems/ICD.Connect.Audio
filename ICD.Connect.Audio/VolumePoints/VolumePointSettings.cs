using System;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.VolumePoints
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class VolumePointSettings : AbstractVolumePointSettings
	{
		private const string FACTORY_NAME = "VolumePoint";

		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(VolumePoint); } }
	}
}
