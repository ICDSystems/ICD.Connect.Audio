using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public sealed class OnkyoAvrPowerControl : AbstractPowerDeviceControl<OnkyoAvrDevice>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public OnkyoAvrPowerControl(OnkyoAvrDevice parent, int id) : base(parent, id)
        { }

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(OnkyoAvrDevice parent)
        {
            base.Subscribe(parent);

            if (parent == null)
                return;
            
            parent.RegisterCommandCallback(eOnkyoCommand.Power, ParseResponse);
            
            parent.OnIsOnlineStateChanged += ParentOnOnIsOnlineStateChanged;

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

            if (parent == null)
                return;
            
            parent.UnregisterCommandCallback(eOnkyoCommand.Power, ParseResponse);
            
            parent.OnIsOnlineStateChanged -= ParentOnOnIsOnlineStateChanged;
        }

        private void ParseResponse(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData)
        {
            if (string.Equals(responseParameter, "00"))
                PowerState = ePowerState.PowerOff;
            else if (string.Equals(responseParameter, "01"))
                PowerState = ePowerState.PowerOn;
            else
                PowerState = ePowerState.Unknown;
        }

        private void ParentOnOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
        {
            if (args.Data)
                Parent.SendCommand(OnkyoIscpCommand.PowerQuery());
        }

        private void Query()
        {
            Parent.SendCommand(OnkyoIscpCommand.PowerQuery());
        }

        /// <summary>
        /// Override to implement the power-on action.
        /// </summary>
        protected override void PowerOnFinal()
        {
            Parent.SendCommand(OnkyoIscpCommand.PowerCommand(true));
        }

        /// <summary>
        /// Override to implement the power-off action.
        /// </summary>
        protected override void PowerOffFinal()
        {
            Parent.SendCommand(OnkyoIscpCommand.PowerCommand(false));
        }
    }
}