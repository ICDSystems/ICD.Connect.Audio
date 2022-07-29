using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Audio.CrestronPro.Swamp
{
    [KrangSettings("SwampAdapter", typeof(SwampAdapter))]
    public sealed class SwampAdapterSettings : AbstractDeviceSettings
    {
        private const string IPID_ELEMENT = "IPID";
        
        private const int MAX_EXPANDERS = 8;
        private const string EXPANDER_TYPE_ELEMENT_TEMPLATE = "Expander{0}Type";

        private readonly Dictionary<int, eExpanderType> m_ExpanderTypes;
        
        public IEnumerable<KeyValuePair<int, eExpanderType>> ExpanderTypes
        {
            get { return m_ExpanderTypes.ToArray(m_ExpanderTypes.Count); }
        }

        #region Expander Type Properties

        public eExpanderType Expander1Type
        {
            get { return GetExpanderType(1); }
            set { SetExpanderType(1, value); }
        }

        public eExpanderType Expander2Type
        {
            get { return GetExpanderType(2); }
            set { SetExpanderType(2, value); }
        }

        public eExpanderType Expander3Type
        {
            get { return GetExpanderType(3); }
            set { SetExpanderType(3, value); }
        }

        public eExpanderType Expander4Type
        {
            get { return GetExpanderType(4); }
            set { SetExpanderType(4, value); }
        }
        public eExpanderType Expander5Type
        {
            get { return GetExpanderType(5); }
            set { SetExpanderType(5, value); }
        }
        public eExpanderType Expander6Type
        {
            get { return GetExpanderType(6); }
            set { SetExpanderType(6, value); }
        }
        public eExpanderType Expander7Type
        {
            get { return GetExpanderType(7); }
            set { SetExpanderType(7, value); }
        }
        public eExpanderType Expander8Type
        {
            get { return GetExpanderType(8); }
            set { SetExpanderType(8, value); }
        }

        #endregion

        [CrestronByteSettingsProperty]
        public byte Ipid { get; set; }

        public SwampAdapterSettings()
        {
            m_ExpanderTypes = new Dictionary<int, eExpanderType>();
        }

        public void SetExpanderTypes(IEnumerable<KeyValuePair<int, eExpanderType>> expanderTypes)
        {
            m_ExpanderTypes.Clear();
            m_ExpanderTypes.AddRange(expanderTypes);
        }

        public void ClearExpanderTypes()
        {
            m_ExpanderTypes.Clear();
        }

        private eExpanderType GetExpanderType(int address)
        {
            if (address < 1 || address > MAX_EXPANDERS)
                throw new ArgumentOutOfRangeException("address");

            eExpanderType type;
            if (!m_ExpanderTypes.TryGetValue(address, out type))
                type = eExpanderType.None;

            return type;
        }

        public void SetExpanderType(int address, eExpanderType type)
        {
            if (type != eExpanderType.None || m_ExpanderTypes.ContainsKey(address))
                m_ExpanderTypes[address] = type;
        }

        /// <summary>
        /// Writes property elements to xml.
        /// </summary>
        /// <param name="writer"></param>
        protected override void WriteElements(IcdXmlTextWriter writer)
        {
            base.WriteElements(writer);

            writer.WriteElementString(IPID_ELEMENT, StringUtils.ToIpIdString(Ipid));
            
            for(int i = 1 ; i <= MAX_EXPANDERS; i++)
            {
                string elementName = string.Format(EXPANDER_TYPE_ELEMENT_TEMPLATE, i);
                writer.WriteElementString(elementName, IcdXmlConvert.ToString(GetExpanderType(i)));
            }

        }

        /// <summary>
        /// Updates the settings from xml.
        /// </summary>
        /// <param name="xml"></param>
        public override void ParseXml(string xml)
        {
            base.ParseXml(xml);

            Ipid = XmlUtils.ReadChildElementContentAsByte(xml, IPID_ELEMENT);
            
            for (int i = 1; i <= MAX_EXPANDERS; i++)
            {
                string elementName = string.Format(EXPANDER_TYPE_ELEMENT_TEMPLATE, i);
                eExpanderType expanderType =
                    XmlUtils.TryReadChildElementContentAsEnum<eExpanderType>(xml, elementName, true) ??
                    eExpanderType.None;
                SetExpanderType(i, expanderType);
            }
        }
    }
}