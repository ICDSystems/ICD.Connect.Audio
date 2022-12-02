using ICD.Connect.API.Nodes;
#if !NETSTANDARD
using Crestron.SimplSharpPro.AudioDistribution;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp.Controls
{
    public sealed class ExpanderZoneSwampVolumeControl : AbstractZoneSwampVolumeControl
    {
        private readonly int m_ExpanderNumber;
        
        #if !NETSTANDARD

        private Expander m_Expander;

        private Expander Expander
        {
            get
            {
                return m_Expander;
            }
            set
            {
                if (m_Expander == value)
                    return;
                
                Unsubscribe(m_Expander);
                m_Expander = value;
                Subscribe(m_Expander);
            }
        }
        
        #endif

        /// <summary>
        /// Gets the human readable name for this control.
        /// </summary>
        public override string Name
        {
            get { return string.Format("SwampVolumeControl|Zone:{1}|Expander:{0}", m_ExpanderNumber, ZoneNumber); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="expanderNumber"></param>
        /// <param name="zoneNumber"></param>
        public ExpanderZoneSwampVolumeControl(SwampAdapter parent, int id, int expanderNumber, int zoneNumber) : base(
            parent, id, zoneNumber)
        {
            m_ExpanderNumber = expanderNumber;
        }
        
        #if !NETSTANDARD

        protected override void SetupZoneFromSwamp()
        {
            if (Swamp == null)
            {
                Expander = null;
                Zone = null;
                return;
            }

            Expander = Swamp.Expanders[(uint)m_ExpanderNumber];
            Zone = Expander == null ? null : Expander.Zones[(uint)ZoneNumber];
        }

        private void Subscribe(Expander expander)
        {
            if (expander == null)
                return;

            expander.ZoneChangeEvent += ZoneParentOnZoneChangeEvent;
        }
        
        private void Unsubscribe(Expander expander)
        {
            if (expander == null)
                return;

            expander.ZoneChangeEvent -= ZoneParentOnZoneChangeEvent;
        }
        
        #endif

        /// <summary>
        /// Calls the delegate for each console status item.
        /// </summary>
        /// <param name="addRow"></param>
        public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
        {
            base.BuildConsoleStatus(addRow);

            addRow("Expander Number", m_ExpanderNumber);
        }
    }
}