using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
    public abstract class AbstractOnkyoAvrPowerControl : AbstractPowerDeviceControl<IOnkyoAvrDevice>
    {
        
        protected abstract eOnkyoCommand PowerCommand { get; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        protected AbstractOnkyoAvrPowerControl(IOnkyoAvrDevice parent, int id) : base(parent, id)
        { }

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(IOnkyoAvrDevice parent)
        {
            base.Subscribe(parent);

            if (parent == null)
                return;
            
            parent.RegisterCommandCallback(PowerCommand, ParseResponse);
            
            parent.OnIsOnlineStateChanged += ParentOnOnIsOnlineStateChanged;

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

            if (parent == null)
                return;
            
            parent.UnregisterCommandCallback(PowerCommand, ParseResponse);
            
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
                Query();
        }

        private void Query()
        {
            Parent.SendCommand(GetPowerQueryCommand());
        }

        /// <summary>
        /// Override to implement the power-on action.
        /// </summary>
        protected override void PowerOnFinal()
        {
            Parent.SendCommand(GetPowerSetCommand(true));
        }

        /// <summary>
        /// Override to implement the power-off action.
        /// </summary>
        protected override void PowerOffFinal()
        {
            Parent.SendCommand(GetPowerSetCommand(false));
        }

        private OnkyoIscpCommand GetPowerQueryCommand()
        {
            return OnkyoIscpCommand.GetQueryCommand(PowerCommand);
        }

        private OnkyoIscpCommand GetPowerSetCommand(bool state)
        {
            return OnkyoIscpCommand.GetSetCommand(PowerCommand, state);
        }
    }
}