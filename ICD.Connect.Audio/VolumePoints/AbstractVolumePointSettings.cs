using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Audio.VolumePoints
{
	[PublicAPI]
	public abstract class AbstractVolumePointSettings : AbstractPointSettings, IVolumePointSettings
	{
		public const float DEFAULT_STEP_LEVEL = 1.0f;
		public const float DEFAULT_STEP_PERCENT = 0.01f;
		public const long DEFAULT_STEP_INTERVAL = 250;

		private const string ELEMENT_VOLUME_RANGE_MODE = "VolumeRepresentation";

		private const string ELEMENT_VOLUME_SAFETY_MIN = "VolumeSafetyMin";
		private const string ELEMENT_VOLUME_SAFETY_MAX = "VolumeSafetyMax";
		private const string ELEMENT_VOLUME_DEFAULT = "VolumeDefault";

		private const string ELEMENT_VOLUME_RAMP_STEP_SIZE = "VolumeRampStepSize";
		private const string ELEMENT_VOLUME_RAMP_INITIAL_STEP_SIZE = "VolumeRampInitialStepSize";
		private const string ELEMENT_VOLUME_RAMP_INTERVAL = "VolumeRampInterval";
		private const string ELEMENT_VOLUME_RAMP_INITIAL_INTERVAL = "VolumeRampInitialInterval";

		private const string ELEMENT_VOLUME_TYPE = "VolumeType";
		private const string ELEMENT_CONTEXT = "Context";
		private const string ELEMENT_MUTE_TYPE = "MuteType";

		#region Properties

		/// <summary>
		/// Determines how the volume safety min, max and default are defined.
		/// </summary>
		public eVolumeRepresentation VolumeRepresentation { get; set; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		public float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		public float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		public float? VolumeDefault { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for each ramp interval.
		/// </summary>
		public float VolumeRampStepSize { get; set; }

		/// <summary>
		/// Gets/sets the percentage or level to increment volume for the first ramp interval.
		/// </summary>
		public float VolumeRampInitialStepSize { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between each volume ramp step.
		/// </summary>
		public long VolumeRampInterval { get; set; }

		/// <summary>
		/// Gets/sets the number of milliseconds between the first and second ramp step.
		/// </summary>
		public long VolumeRampInitialInterval { get; set; }

		/// <summary>
		/// Determines the contextual availability of this volume point.
		/// </summary>
		public eVolumePointContext Context { get; set; }

		/// <summary>
		/// Determines what muting this volume point will do (mute audio output, mute microphones, etc).
		/// </summary>
		public eMuteType MuteType { get; set; }

		#endregion

		#region Serialization

		/// <summary>
		/// Write property elements to xml
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_VOLUME_RANGE_MODE, IcdXmlConvert.ToString(VolumeRepresentation));
			
			WriteVolume(writer, ELEMENT_VOLUME_SAFETY_MIN, VolumeRepresentation, VolumeSafetyMin);
			WriteVolume(writer, ELEMENT_VOLUME_SAFETY_MAX, VolumeRepresentation, VolumeSafetyMax);
			WriteVolume(writer, ELEMENT_VOLUME_DEFAULT, VolumeRepresentation, VolumeDefault);
			WriteVolume(writer, ELEMENT_VOLUME_RAMP_STEP_SIZE, VolumeRepresentation, VolumeRampStepSize);
			WriteVolume(writer, ELEMENT_VOLUME_RAMP_INITIAL_STEP_SIZE, VolumeRepresentation, VolumeRampInitialStepSize);

			writer.WriteElementString(ELEMENT_VOLUME_RAMP_INTERVAL, IcdXmlConvert.ToString(VolumeRampInterval));
			writer.WriteElementString(ELEMENT_VOLUME_RAMP_INITIAL_INTERVAL, IcdXmlConvert.ToString(VolumeRampInitialInterval));
			writer.WriteElementString(ELEMENT_CONTEXT, IcdXmlConvert.ToString(Context));
			writer.WriteElementString(ELEMENT_MUTE_TYPE, IcdXmlConvert.ToString(MuteType));
		}

		/// <summary>
		/// Instantiate volume point settings from an xml element
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			VolumeRepresentation =
				XmlUtils.TryReadChildElementContentAsEnum<eVolumeRepresentation>(xml, ELEMENT_VOLUME_RANGE_MODE, true) ??
				eVolumeRepresentation.Percent;

			VolumeSafetyMin = ReadVolume(xml, ELEMENT_VOLUME_SAFETY_MIN, VolumeRepresentation);
			VolumeSafetyMax = ReadVolume(xml, ELEMENT_VOLUME_SAFETY_MAX, VolumeRepresentation);
			VolumeDefault = ReadVolume(xml, ELEMENT_VOLUME_DEFAULT, VolumeRepresentation);
			VolumeRampStepSize = ReadVolume(xml, ELEMENT_VOLUME_RAMP_STEP_SIZE, VolumeRepresentation) ?? GetDefaultStepSize(VolumeRepresentation);
			VolumeRampInitialStepSize = ReadVolume(xml, ELEMENT_VOLUME_RAMP_INITIAL_STEP_SIZE, VolumeRepresentation) ?? GetDefaultStepSize(VolumeRepresentation);

			VolumeRampInterval = XmlUtils.TryReadChildElementContentAsLong(xml, ELEMENT_VOLUME_RAMP_INTERVAL) ?? DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = XmlUtils.TryReadChildElementContentAsLong(xml, ELEMENT_VOLUME_RAMP_INITIAL_INTERVAL) ?? DEFAULT_STEP_INTERVAL;

			// Backwards compatability for "VolumeType" element.
			Context = XmlUtils.TryReadChildElementContentAsEnum<eVolumePointContext>(xml, ELEMENT_CONTEXT, true) ??
			          XmlUtils.TryReadChildElementContentAsEnum<eVolumePointContext>(xml, ELEMENT_VOLUME_TYPE, true) ??
			          eVolumePointContext.Room;

			MuteType = XmlUtils.TryReadChildElementContentAsEnum<eMuteType>(xml, ELEMENT_MUTE_TYPE, true) ?? eMuteType.RoomAudio;
		}

		#endregion

		#region Private Methods

		private static void WriteVolume(IcdXmlTextWriter writer, string element, eVolumeRepresentation mode, float? value)
		{
			switch (mode)
			{
				case eVolumeRepresentation.Level:
					writer.WriteElementString(element, IcdXmlConvert.ToString(value));
					break;
				case eVolumeRepresentation.Percent:
					writer.WriteElementString(element, IcdXmlConvert.ToString(value * 100));
					break;
				default:
					throw new ArgumentOutOfRangeException("mode");
			}
		}

		private static float? ReadVolume(string xml, string element, eVolumeRepresentation mode)
		{
			switch (mode)
			{
				case eVolumeRepresentation.Level:
					return XmlUtils.TryReadChildElementContentAsFloat(xml, element);
				case eVolumeRepresentation.Percent:
					return XmlUtils.TryReadChildElementContentAsFloat(xml, element) / 100;
				default:
					throw new ArgumentOutOfRangeException("mode");
			}
		}

		private static float GetDefaultStepSize(eVolumeRepresentation mode)
		{
			switch (mode)
			{
				case eVolumeRepresentation.Level:
					return DEFAULT_STEP_LEVEL;
				case eVolumeRepresentation.Percent:
					return DEFAULT_STEP_PERCENT;
				default:
					throw new ArgumentOutOfRangeException("mode");
			}
		}

		#endregion
	}
}
