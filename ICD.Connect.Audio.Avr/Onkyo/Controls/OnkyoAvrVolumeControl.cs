using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class OnkyoAvrVolumeControl : AbstractVolumeDeviceControl<OnkyoAvrDevice>
    {
        /// <summary>
        /// Gets the minimum supported volume level.
        /// </summary>
        public override float VolumeLevelMin
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the maximum supported volume level.
        /// todo: Might need to have better support of changing max volume
        /// </summary>
        public override float VolumeLevelMax
        {
            get { return Parent.MaxVolume; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public OnkyoAvrVolumeControl(OnkyoAvrDevice parent, int id) : base(parent, id)
        {
            SupportedVolumeFeatures = eVolumeFeatures.Mute |
                                      eVolumeFeatures.MuteAssignment |
                                      eVolumeFeatures.MuteFeedback |
                                      eVolumeFeatures.Volume |
                                      eVolumeFeatures.VolumeAssignment |
                                      eVolumeFeatures.VolumeFeedback;
        }

        protected override bool GetControlAvailable()
        {
            return Parent.ControlsAvailable && Parent.IsOnline && Parent.PowerState == ePowerState.PowerOn;
        }

        /// <summary>
        /// Sets the mute state.
        /// </summary>
        /// <param name="mute"></param>
        public override void SetIsMuted(bool mute)
        {
            Parent.SendCommand(OnkyoIscpCommand.MuteCommand(mute));
        }

        /// <summary>
        /// Toggles the current mute state.
        /// </summary>
        public override void ToggleIsMuted()
        {
            Parent.SendCommand(OnkyoIscpCommand.MuteToggle());
        }

        /// <summary>
        /// Sets the raw volume level in the device volume representation.
        /// </summary>
        /// <param name="level"></param>
        public override void SetVolumeLevel(float level)
        {
            int levelInt = Convert.ToInt32(MathUtils.Clamp(level, VolumeLevelMin, VolumeLevelMax));
            Parent.SendCommand(OnkyoIscpCommand.VolumeSet(levelInt));
        }

        /// <summary>
        /// Raises the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeIncrement()
        {
            Parent.SendCommand(OnkyoIscpCommand.VolumeIncrement());
        }

        /// <summary>
        /// Lowers the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeDecrement()
        {
            Parent.SendCommand(OnkyoIscpCommand.VolumeDecrement());
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

        private void Query()
        {
            Parent.SendCommand(OnkyoIscpCommand.MuteQuery());
            Parent.SendCommand(OnkyoIscpCommand.VolumeQuery());
        }
        
        #region Parent Callbacks

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(OnkyoAvrDevice parent)
        {
            base.Subscribe(parent);
            
            parent.RegisterCommandCallback(eOnkyoCommand.Volume, VolumeResponseCallback);
            parent.RegisterCommandCallback(eOnkyoCommand.Mute, MuteResponseCallback);
            
            parent.OnIsOnlineStateChanged += ParentOnIsOnlineStateChanged;
            parent.OnControlsAvailableChanged += ParentOnControlsAvailableChanged;
            parent.OnPowerStateChange += ParentOnPowerStateChange;
            
            if (parent.IsOnline)
                Query();
        }

        /// <summary>
        /// Unsubscribe from the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Unsubscribe(OnkyoAvrDevice parent)
        {
            base.Unsubscribe(parent);
            
            parent.UnregisterCommandCallback(eOnkyoCommand.Volume, VolumeResponseCallback);
            parent.UnregisterCommandCallback(eOnkyoCommand.Mute, MuteResponseCallback);
            
            parent.OnIsOnlineStateChanged -= ParentOnIsOnlineStateChanged;
            parent.OnControlsAvailableChanged -= ParentOnControlsAvailableChanged;
            parent.OnPowerStateChange -= ParentOnPowerStateChange;
        }

        private void ParentOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
        {
            UpdateCachedControlAvailable();
            if (args.Data)
                Query();
        }
        
        private void ParentOnPowerStateChange(object sender, PowerDeviceControlPowerStateApiEventArgs e)
        {
            UpdateCachedControlAvailable();
        }

        private void ParentOnControlsAvailableChanged(object sender, DeviceBaseControlsAvailableApiEventArgs e)
        {
            UpdateCachedControlAvailable();
        }

        private void VolumeResponseCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData)
        {
            if (string.Equals(responseParameter, OnkyoIscpCommand.ERROR_PARAMETER))
            {
                Logger.Log(eSeverity.Error, "N/A Response to command {0}",sentData.Serialize());
                return;
            }
            
            VolumeLevel = StringUtils.FromIpIdString(responseParameter);
        }

        private void MuteResponseCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData)
        {
            if (string.Equals(responseParameter, OnkyoIscpCommand.ERROR_PARAMETER))
            {
                Logger.Log(eSeverity.Error, "N/A Response to command {0}", sentData.Serialize());
                return;
            }

            IsMuted = string.Equals(responseParameter, "01");
        }

        #endregion
    }
}