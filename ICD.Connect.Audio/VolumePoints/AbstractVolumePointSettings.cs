using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Audio.VolumePoints
{
	[PublicAPI]
	public abstract class AbstractVolumePointSettings : AbstractPointSettings, IVolumePointSettings
	{
		public const float DEFAULT_STEP = 1.0f;
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
		private const string ELEMENT_PRIVACY_MUTE_MASK = "PrivacyMuteMask";

		private const string ELEMENT_INHIBIT_AUTO_DEFAULT_VOLUME = "InhibitAutoDefaultVolume";

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

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		public ePrivacyMuteFeedback PrivacyMuteMask { get; set; }

		/// <summary>
		/// If enabled, prevents default volume from getting set on the control automatically
		/// Specific implementaitons may set default volume under other conditions
		/// </summary>
		public bool InhibitAutoDefaultVolume { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractVolumePointSettings()
		{
			VolumeRepresentation = eVolumeRepresentation.Percent;
			VolumeRampStepSize = DEFAULT_STEP;
			VolumeRampInitialStepSize = DEFAULT_STEP;
			VolumeRampInterval = DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = DEFAULT_STEP_INTERVAL;
			Context = eVolumePointContext.Room;
			MuteType = eMuteType.RoomAudio;
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
		}

		#region Serialization

		/// <summary>
		/// Write property elements to xml
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_VOLUME_RANGE_MODE, IcdXmlConvert.ToString(VolumeRepresentation));

			writer.WriteElementString(ELEMENT_VOLUME_SAFETY_MIN, IcdXmlConvert.ToString(VolumeSafetyMin));
			writer.WriteElementString(ELEMENT_VOLUME_SAFETY_MAX,IcdXmlConvert.ToString(VolumeSafetyMax));
			writer.WriteElementString(ELEMENT_VOLUME_DEFAULT, IcdXmlConvert.ToString(VolumeDefault));
			writer.WriteElementString(ELEMENT_VOLUME_RAMP_STEP_SIZE, IcdXmlConvert.ToString(VolumeRampStepSize));
			writer.WriteElementString(ELEMENT_VOLUME_RAMP_INITIAL_STEP_SIZE, IcdXmlConvert.ToString(VolumeRampInitialStepSize));

			writer.WriteElementString(ELEMENT_VOLUME_RAMP_INTERVAL, IcdXmlConvert.ToString(VolumeRampInterval));
			writer.WriteElementString(ELEMENT_VOLUME_RAMP_INITIAL_INTERVAL, IcdXmlConvert.ToString(VolumeRampInitialInterval));
			writer.WriteElementString(ELEMENT_CONTEXT, IcdXmlConvert.ToString(Context));
			writer.WriteElementString(ELEMENT_MUTE_TYPE, IcdXmlConvert.ToString(MuteType));
			writer.WriteElementString(ELEMENT_PRIVACY_MUTE_MASK, IcdXmlConvert.ToString(PrivacyMuteMask));
			writer.WriteElementString(ELEMENT_INHIBIT_AUTO_DEFAULT_VOLUME, IcdXmlConvert.ToString(InhibitAutoDefaultVolume));
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

			VolumeSafetyMin = XmlUtils.TryReadChildElementContentAsFloat(xml, ELEMENT_VOLUME_SAFETY_MIN);
			VolumeSafetyMax = XmlUtils.TryReadChildElementContentAsFloat(xml, ELEMENT_VOLUME_SAFETY_MAX);
			VolumeDefault = XmlUtils.TryReadChildElementContentAsFloat(xml, ELEMENT_VOLUME_DEFAULT);
			VolumeRampStepSize = XmlUtils.TryReadChildElementContentAsFloat(xml, ELEMENT_VOLUME_RAMP_STEP_SIZE) ?? DEFAULT_STEP;
			VolumeRampInitialStepSize = XmlUtils.TryReadChildElementContentAsFloat(xml, ELEMENT_VOLUME_RAMP_INITIAL_STEP_SIZE) ?? DEFAULT_STEP;

			VolumeRampInterval = XmlUtils.TryReadChildElementContentAsLong(xml, ELEMENT_VOLUME_RAMP_INTERVAL) ?? DEFAULT_STEP_INTERVAL;
			VolumeRampInitialInterval = XmlUtils.TryReadChildElementContentAsLong(xml, ELEMENT_VOLUME_RAMP_INITIAL_INTERVAL) ?? DEFAULT_STEP_INTERVAL;

			// Backwards compatibility for "VolumeType" element.
			Context = XmlUtils.TryReadChildElementContentAsEnum<eVolumePointContext>(xml, ELEMENT_CONTEXT, true) ??
			          XmlUtils.TryReadChildElementContentAsEnum<eVolumePointContext>(xml, ELEMENT_VOLUME_TYPE, true) ??
			          eVolumePointContext.Room;

			MuteType = XmlUtils.TryReadChildElementContentAsEnum<eMuteType>(xml, ELEMENT_MUTE_TYPE, true) ?? eMuteType.RoomAudio;
			PrivacyMuteMask =
				XmlUtils.TryReadChildElementContentAsEnum<ePrivacyMuteFeedback>(xml, ELEMENT_PRIVACY_MUTE_MASK, true) ??
				ePrivacyMuteFeedback.Set;
			InhibitAutoDefaultVolume = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_INHIBIT_AUTO_DEFAULT_VOLUME) ??
			                           false;
		}

		#endregion
	}
}
