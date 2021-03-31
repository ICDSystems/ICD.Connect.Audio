using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Devices.GenericAmp
{
	[KrangSettings("GenericAmpDevice", typeof(GenericAmpDevice))]
	public sealed class GenericAmpDeviceSettings : AbstractDeviceSettings
	{
		private const string INPUTS_ELEMENT = "Inputs";
		private const string PAIR_ELEMENT = "Pair";
		private const string INPUT_ELEMENT = "Input";
		private const string VOLUME_POINT_ELEMENT = "VolumePoint";

		private readonly Dictionary<int, int> m_InputVolumePointIds;

		/// <summary>
		/// Constructor.
		/// </summary>
		public GenericAmpDeviceSettings()
		{
			m_InputVolumePointIds = new Dictionary<int, int>();
		}

		/// <summary>
		/// Gets the volume point id for each input.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<int, int>> GetInputVolumePointIds()
		{
			return m_InputVolumePointIds.ToArray(m_InputVolumePointIds.Count);
		}

		/// <summary>
		/// Sets the volume point id for each input.
		/// </summary>
		/// <returns></returns>
		public void SetInputVolumePointIds(IEnumerable<KeyValuePair<int, int>> inputVolumePointIds)
		{
			if (inputVolumePointIds == null)
				throw new ArgumentNullException("inputVolumePointIds");

			m_InputVolumePointIds.Clear();

			foreach (KeyValuePair<int, int> item in inputVolumePointIds)
			{
				if (m_InputVolumePointIds.ContainsKey(item.Key))
				{
					Logger.AddEntry(eSeverity.Error, "{0} unable to add volume point id for duplicate input {1}", GetType().Name,
					                item.Key);
					continue;
				}

				m_InputVolumePointIds.Add(item.Key, item.Value);
			}
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			XmlUtils.WriteDictToXml(writer, GetInputVolumePointIds(), INPUTS_ELEMENT, PAIR_ELEMENT, INPUT_ELEMENT,
			                        VOLUME_POINT_ELEMENT);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<KeyValuePair<int, int>> inputVolumePointIds =
				XmlUtils.ReadDictFromXml(xml, INPUTS_ELEMENT, PAIR_ELEMENT, INPUT_ELEMENT, VOLUME_POINT_ELEMENT,
				                         key => XmlUtils.TryReadElementContentAsInt(key) ?? 0,
				                         value => XmlUtils.TryReadElementContentAsInt(value) ?? 0);

			SetInputVolumePointIds(inputVolumePointIds);
		}
	}
}
