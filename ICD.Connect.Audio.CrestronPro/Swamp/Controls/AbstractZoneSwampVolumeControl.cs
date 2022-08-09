using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
#if !NETSTANDARD
using Crestron.SimplSharpPro.AudioDistribution;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp.Controls
{
    public abstract class AbstractZoneSwampVolumeControl : AbstractVolumeDeviceControl<SwampAdapter>
    {
        protected int ZoneNumber { get; private set; }

#if !NETSTANDARD

        [CanBeNull]
        private Swamp24x8 m_Swamp;

        [CanBeNull]
        protected Swamp24x8 Swamp
        {
            get { return m_Swamp; }
            private set
            {
                if (m_Swamp == value)
                    return;

                Unsubscribe(m_Swamp);
                m_Swamp = value;
                Subscribe(m_Swamp);

                SetupZoneFromSwamp();
            }
        }

        [CanBeNull]
        private Zone m_Zone;

        [CanBeNull]
        protected Zone Zone
        {
            get { return m_Zone; }
            set
            {
                if (m_Zone == value)
                    return;

                m_Zone = value;
                
                UpdateSupportedFeatures();
                UpdateVolumeFeedback();
                UpdateMuteFeedback();
                UpdateCachedControlAvailable();
            }
        }
#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="zoneNumber"></param>
        protected AbstractZoneSwampVolumeControl(SwampAdapter parent, int id, int zoneNumber) :
            base(parent, id)
        {
            ZoneNumber = zoneNumber;
        }

#if !NETSTANDARD

        protected override bool GetControlAvailable()
        {
            return base.GetControlAvailable() && m_Zone != null;
        }

        #region Parent Callbacks

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(SwampAdapter parent)
        {
            base.Subscribe(parent);

            parent.OnSwampChanged += ParentOnOnSwampChanged;

            Swamp = parent.Swamp;
        }

        /// <summary>
        /// Unsubscribe from the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Unsubscribe(SwampAdapter parent)
        {
            base.Unsubscribe(parent);

            parent.OnSwampChanged -= ParentOnOnSwampChanged;

            Swamp = null;
        }

        private void ParentOnOnSwampChanged(object sender, GenericEventArgs<Swamp24x8> args)
        {
            // This is setup to handle if the swamp changes,
            // but this should never happen, since the zone comes from the swamp!
            Swamp = args.Data;
        }

        #endregion

        #region Swamp Callbacks

        protected virtual void Subscribe(Swamp24x8 swamp)
        {
        }

        protected virtual void Unsubscribe(Swamp24x8 swamp)
        {
        }

        protected void ZoneParentOnZoneChangeEvent(object sender, ZoneEventArgs args)
        {
            if (Zone == null || Zone.Number != args.Zone.Number)
                return;

            switch (args.EventId)
            {
                case ZoneEventIds.VolumeFeedbackEventId:
                    UpdateVolumeFeedback();
                    break;
                case ZoneEventIds.MuteOnFeedbackEventId:
                    UpdateMuteFeedback();
                    break;
                case ZoneEventIds.SourceFeedbackEventId:
                    // Volume control doesn't support anything with no source
                    UpdateSupportedFeatures();
                    break;
                case ZoneEventIds.MinVolumeFeedbackEventId:
                case ZoneEventIds.MaxVolumeFeedbackEventId:
                    // Todo: Do we need to handle this?
                    break;
            }


        }

        #endregion


        #region Zone

        protected abstract void SetupZoneFromSwamp();


        private void UpdateSupportedFeatures()
        {
            // If there is no zone, or the zone is off, no features
            if (Zone == null || Zone.SourceFeedback.UShortValue == 0)
                SupportedVolumeFeatures = eVolumeFeatures.None;
            else
            {
                SupportedVolumeFeatures = eVolumeFeatures.Volume |
                                          eVolumeFeatures.MuteAssignment |
                                          eVolumeFeatures.MuteFeedback |
                                          eVolumeFeatures.VolumeAssignment |
                                          eVolumeFeatures.VolumeFeedback;
            }
        }

        private void UpdateVolumeFeedback()
        {
            if (Zone != null)
                VolumeLevel = Zone.VolumeFeedback.UShortValue;
        }

        private void UpdateMuteFeedback()
        {
            if (Zone != null)
                IsMuted = Zone.MuteOnFeedback.BoolValue;
        }

        #endregion

#endif

        /// <summary>
        /// Gets the minimum supported volume level.
        /// </summary>
        public override float VolumeLevelMin
        {
            // The SWAMP scales the volume between it's min and max itself
            get { return ushort.MinValue; }
        }

        /// <summary>
        /// Gets the maximum supported volume level.
        /// </summary>
        public override float VolumeLevelMax
        {
            // The SWAMP scales the volume between it's min and max itself
            get { return ushort.MaxValue; }
        }

        /// <summary>
        /// Sets the mute state.
        /// </summary>
        /// <param name="mute"></param>
        public override void SetIsMuted(bool mute)
        {
#if !NETSTANDARD
            if (Zone == null)
                return;

            if (mute)
                Zone.MuteOn();
            else
                Zone.MuteOff();
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Toggles the current mute state.
        /// </summary>
        public override void ToggleIsMuted()
        {
            SetIsMuted(!IsMuted);
        }

        /// <summary>
        /// Sets the raw volume level in the device volume representation.
        /// </summary>
        /// <param name="level"></param>
        public override void SetVolumeLevel(float level)
        {
#if !NETSTANDARD
            if (Zone != null)
                Zone.Volume.UShortValue = Convert.ToUInt16(level);
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Raises the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeIncrement()
        {
            SetVolumeLevel(VolumeLevel + 1);
        }

        /// <summary>
        /// Lowers the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeDecrement()
        {
            SetVolumeLevel(VolumeLevel - 1);
        }

        /// <summary>
        /// Starts ramping the volume, and continues until stop is called or the timeout is reached.
        /// If already ramping the current timeout is updated to the new timeout duration.
        /// </summary>
        /// <param name="increment">Increments the volume if true, otherwise decrements.</param>
        /// <param name="timeout"></param>
        public override void VolumeRamp(bool increment, long timeout)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Stops any current ramp up/down in progress.
        /// </summary>
        public override void VolumeRampStop()
        {
            throw new NotSupportedException();
        }


        #region Console

        /// <summary>
        /// Calls the delegate for each console status item.
        /// </summary>
        /// <param name="addRow"></param>
        public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
        {
            base.BuildConsoleStatus(addRow);
#if !NETSTANDARD
            if (Zone != null)
            {
                addRow("Swamp Zone Number", Zone.Number);
                addRow("Swamp Zone Name", Zone.Name.StringValue);
                addRow("Startup Volume", Zone.StartupVolumeFeedback.UShortValue);
            }
#endif
            addRow("Zone Number", ZoneNumber);
        }

        #endregion
    }
}