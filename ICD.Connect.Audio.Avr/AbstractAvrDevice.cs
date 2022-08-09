using ICD.Connect.Devices;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Avr
{
    public abstract class AbstractAvrDevice<T>: AbstractDevice<T>, IAvrDevice
    where T : IAvrDeviceSettings, new()
    {
        /// <summary>
        /// When true, routing to an output will power the associated zone on, and unrouting will power it off.
        /// </summary>
        public bool SetZonePowerWithRouting { get; private set; }

        /// <summary>
        /// Override to clear the instance settings.
        /// </summary>
        protected override void ClearSettingsFinal()
        {
            base.ClearSettingsFinal();

            SetZonePowerWithRouting = true;
        }

        /// <summary>
        /// Override to apply properties to the settings instance.
        /// </summary>
        /// <param name="settings"></param>
        protected override void CopySettingsFinal(T settings)
        {
            base.CopySettingsFinal(settings);

            settings.SetZonePowerWithRouting = SetZonePowerWithRouting;
        }

        /// <summary>
        /// Override to apply settings to the instance.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
        {
            base.ApplySettingsFinal(settings, factory);

            SetZonePowerWithRouting = settings.SetZonePowerWithRouting;
        }
    }
}