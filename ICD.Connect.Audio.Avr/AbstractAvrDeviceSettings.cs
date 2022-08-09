using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Avr
{
    public abstract class AbstractAvrDeviceSettings : AbstractDeviceSettings, IAvrDeviceSettings
    {
        private const string SET_ZONE_POWER_WITH_ROUTING_ELEMENT = "SetZonePowerWithRouting";

        /// <summary>
        /// When true, routing to an output will power the associated zone on, and unrouting will power it off.
        /// </summary>
        public bool SetZonePowerWithRouting { get; set; }

        /// <summary>
        /// Updates the settings from xml.
        /// </summary>
        /// <param name="xml"></param>
        public override void ParseXml(string xml)
        {
            base.ParseXml(xml);

            SetZonePowerWithRouting =
                XmlUtils.TryReadChildElementContentAsBoolean(xml, SET_ZONE_POWER_WITH_ROUTING_ELEMENT) ?? true;
        }


        /// <summary>
        /// Writes property elements to xml.
        /// </summary>
        /// <param name="writer"></param>
        protected override void WriteElements(IcdXmlTextWriter writer)
        {
            base.WriteElements(writer);
            
            writer.WriteElementString(SET_ZONE_POWER_WITH_ROUTING_ELEMENT, IcdXmlConvert.ToString(SetZonePowerWithRouting));
        }
    }
}