using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public abstract class AbstractOnkyoAvrVolumeControl : AbstractVolumeDeviceControl<IOnkyoAvrDevice>
    {
        /// <summary>
        /// Default volume for zones other than main zone
        /// </summary>
        protected const int OTHER_ZONE_DEFAULT_VOLUME = 80;
        
        private readonly IPowerDeviceControl m_PowerControl;
        protected abstract eOnkyoCommand VolumeCommand { get; }
        protected abstract eOnkyoCommand MuteCommand { get; }
        
        /// <summary>
        /// Gets the minimum supported volume level.
        /// </summary>
        public override float VolumeLevelMin
        {
            get { return 0; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="powerControl"></param>
        protected AbstractOnkyoAvrVolumeControl(IOnkyoAvrDevice parent, int id, [NotNull] IPowerDeviceControl powerControl) : base(parent, id)
        {
            if (powerControl == null)
                throw new ArgumentNullException("powerControl");

            m_PowerControl = powerControl;
            Subscribe(powerControl);
            UpdateCachedControlAvailable();
            
            SupportedVolumeFeatures = eVolumeFeatures.Mute |
                                      eVolumeFeatures.MuteAssignment |
                                      eVolumeFeatures.MuteFeedback |
                                      eVolumeFeatures.Volume |
                                      eVolumeFeatures.VolumeAssignment |
                                      eVolumeFeatures.VolumeFeedback;
        }

        /// <summary>
        /// Override to release resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void DisposeFinal(bool disposing)
        {
            base.DisposeFinal(disposing);
            Unsubscribe(m_PowerControl);
        }


        protected sealed override void UpdateCachedControlAvailable()
        {
            // Sealed method to prevent Virtual Member Call In Constructor warnings
            base.UpdateCachedControlAvailable();
        }

        protected override bool GetControlAvailable()
        {
            return Parent.ControlsAvailable && Parent.IsOnline &&
                   (m_PowerControl == null || m_PowerControl.PowerState == ePowerState.PowerOn);
        }

        /// <summary>
        /// Sets the mute state.
        /// </summary>
        /// <param name="mute"></param>
        public override void SetIsMuted(bool mute)
        {
            Parent.SendCommand(GetMuteSetCommand(mute));
        }

        /// <summary>
        /// Toggles the current mute state.
        /// </summary>
        public override void ToggleIsMuted()
        {
            Parent.SendCommand(GetMuteToggleCommand());
        }

        /// <summary>
        /// Sets the raw volume level in the device volume representation.
        /// </summary>
        /// <param name="level"></param>
        public override void SetVolumeLevel(float level)
        {
            int levelInt = Convert.ToInt32(MathUtils.Clamp(level, VolumeLevelMin, VolumeLevelMax));
            Parent.SendCommand(GetVolumeSetCommand(levelInt));
        }

        /// <summary>
        /// Raises the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeIncrement()
        {
            Parent.SendCommand(GetVolumeIncrementCommand());
        }

        /// <summary>
        /// Lowers the volume one time
        /// Amount of the change varies between implementations - typically "1" raw unit
        /// </summary>
        public override void VolumeDecrement()
        {
            Parent.SendCommand(GetVolumeDecrementCommand());
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
            Parent.SendCommand(GetMuteQueryCommand());
            Parent.SendCommand(GetVolumeQueryCommand());
        }

        private OnkyoIscpCommand GetMuteSetCommand(bool mute)
        {
            return OnkyoIscpCommand.GetSetCommand(MuteCommand, mute);
        }

        private OnkyoIscpCommand GetMuteToggleCommand()
        {
            return OnkyoIscpCommand.GetToggleCommand(MuteCommand);
        }

        private OnkyoIscpCommand GetMuteQueryCommand()
        {
            return OnkyoIscpCommand.GetQueryCommand(MuteCommand);
        }

        private OnkyoIscpCommand GetVolumeSetCommand(int volume)
        {
            return OnkyoIscpCommand.GetSetCommand(VolumeCommand, volume);
        }

        private OnkyoIscpCommand GetVolumeQueryCommand()
        {
            return OnkyoIscpCommand.GetQueryCommand(VolumeCommand);
        }

        private OnkyoIscpCommand GetVolumeIncrementCommand()
        {
            return OnkyoIscpCommand.GetUpCommand(VolumeCommand);
        }

        private OnkyoIscpCommand GetVolumeDecrementCommand()
        {
            return OnkyoIscpCommand.GetDownCommand(VolumeCommand);
        }
        
        #region Parent Callbacks

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(IOnkyoAvrDevice parent)
        {
            base.Subscribe(parent);
            
            parent.RegisterCommandCallback(VolumeCommand, VolumeResponseCallback);
            parent.RegisterCommandCallback(MuteCommand, MuteResponseCallback);
            
            parent.OnIsOnlineStateChanged += ParentOnIsOnlineStateChanged;
            parent.OnControlsAvailableChanged += ParentOnControlsAvailableChanged;

            if (parent.IsOnline)
                Query();
        }

        /// <summary>
        /// Unsubscribe from the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Unsubscribe(IOnkyoAvrDevice parent)
        {
            base.Unsubscribe(parent);
            
            parent.UnregisterCommandCallback(VolumeCommand, VolumeResponseCallback);
            parent.UnregisterCommandCallback(MuteCommand, MuteResponseCallback);
            
            parent.OnIsOnlineStateChanged -= ParentOnIsOnlineStateChanged;
            parent.OnControlsAvailableChanged -= ParentOnControlsAvailableChanged;
        }

        private void ParentOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
        {
            UpdateCachedControlAvailable();
            if (args.Data)
                Query();
        }

        private void ParentOnControlsAvailableChanged(object sender, DeviceBaseControlsAvailableApiEventArgs e)
        {
            UpdateCachedControlAvailable();
        }

        private void VolumeResponseCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData)
        {
            if (string.Equals(responseParameter, OnkyoIscpCommand.ERROR_PARAMETER))
            {
                string sentCommand = sentData == null ? "[Unknown Command]" : sentData.Serialize();
                Logger.Log(eSeverity.Error, "N/A Response to command {0}",sentCommand);
                return;
            }
            
            VolumeLevel = StringUtils.FromIpIdString(responseParameter);
        }

        private void MuteResponseCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData)
        {
            if (string.Equals(responseParameter, OnkyoIscpCommand.ERROR_PARAMETER))
            {
                string sentCommand = sentData == null ? "[Unknown Command]" : sentData.Serialize();
                Logger.Log(eSeverity.Error, "N/A Response to command {0}", sentCommand);
                return;
            }

            IsMuted = string.Equals(responseParameter, "01");
        }

        #endregion

        #region PowerControl Callbacks

        private void Subscribe(IPowerDeviceControl powerControl)
        {
            if (powerControl == null)
                return;

            powerControl.OnPowerStateChanged += PowerControlOnPowerStateChange;
        }

        private void Unsubscribe(IPowerDeviceControl powerControl)
        {
            if (powerControl == null)
                return;

            powerControl.OnPowerStateChanged -= PowerControlOnPowerStateChange;
        }
        
        private void PowerControlOnPowerStateChange(object sender, PowerDeviceControlPowerStateApiEventArgs e)
        {
            UpdateCachedControlAvailable();
        }

        #endregion
    }
}