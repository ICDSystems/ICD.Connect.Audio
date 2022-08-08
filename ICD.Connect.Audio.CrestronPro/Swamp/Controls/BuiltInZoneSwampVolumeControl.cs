#if !NETSTANDARD
using Crestron.SimplSharpPro.AudioDistribution;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp.Controls
{
    public class BuiltInZoneSwampVolumeControl : AbstractZoneSwampVolumeControl
    {
        /// <summary>
        /// Gets the human readable name for this control.
        /// </summary>
        public override string Name
        {
            get { return string.Format("SwampVolumeControl|Zone:{0}", ZoneNumber); }
        }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="zoneNumber"></param>
        public BuiltInZoneSwampVolumeControl(SwampAdapter parent, int id, int zoneNumber) : base(parent, id, zoneNumber)
        { }

#if !NETSTANDARD
        protected override void Subscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.ZoneChangeEvent += ZoneParentOnZoneChangeEvent;
        }

        protected override void Unsubscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.ZoneChangeEvent -= ZoneParentOnZoneChangeEvent;
        }

        protected override void SetupZoneFromSwamp()
        {
            Zone = Swamp == null ? null : Swamp.Zones[(uint)ZoneNumber];
        }
#endif
    }
}