using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Settings;
#if !NETSTANDARD
using ICD.Common.Properties;
using ICD.Connect.Misc.CrestronPro;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Misc.CrestronPro.Utils;
using Crestron.SimplSharpPro.AudioDistribution;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.GeneralIO;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp
{
    public sealed class SwampAdapter : AbstractDevice<SwampAdapterSettings>
    {

#if !NETSTANDARD
        [CanBeNull] private Swamp24x8 m_Swamp;
#endif

        /// <summary>
        /// Gets the current online status of the device.
        /// </summary>
        /// <returns></returns>
        protected override bool GetIsOnlineStatus()
        {
#if !NETSTANDARD
            return m_Swamp != null && m_Swamp.IsOnline;
#else
            return false;
#endif
        }

#if !NETSTANDARD
        /// <summary>
        /// Sets the wrapped device.
        /// </summary>
        /// <param name="device"></param>
        private void SetDevice(Swamp24x8 device)
        {
            if (device == m_Swamp)
                return;

            Unsubscribe(m_Swamp);

            if (m_Swamp != null)
                GenericBaseUtils.TearDown(m_Swamp);

            m_Swamp = device;

            eDeviceRegistrationUnRegistrationResponse result;
            if (m_Swamp != null && !GenericBaseUtils.SetUp(m_Swamp, this, out result))
                Logger.Log(eSeverity.Error, "Unable to register {0} - {1}", m_Swamp.GetType().Name, result);

            Subscribe(m_Swamp);
            UpdateCachedOnlineStatus();
        }

        #region Device Callbacks

        private void Subscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.OnlineStatusChange += SwampOnOnlineStatusChange;
        }

        private void Unsubscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.OnlineStatusChange -= SwampOnOnlineStatusChange;
        }

        private void SwampOnOnlineStatusChange(GenericBase device, OnlineOfflineEventArgs args)
        {
            UpdateCachedOnlineStatus();
        }

        #endregion

#endif

        #region Settings

        /// <summary>
        /// Override to apply settings to the instance.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        protected override void ApplySettingsFinal(SwampAdapterSettings settings, IDeviceFactory factory)
        {

#if !NETSTANDARD

            base.ApplySettingsFinal(settings, factory);

            Swamp24x8 device = null;
            try
            {
                device = new Swamp24x8(settings.Ipid, ProgramInfo.ControlSystem);
            }
            catch (ArgumentException e)
            {
                Logger.Log(eSeverity.Error, "Failed to instantiate {0} with Cresnet ID {1} - {2}",
                    typeof(CenOdtCPoe).Name, settings.Ipid, e.Message);
            }

            // Instantiate expanders before setting up device
            foreach (var kvp in settings.ExpanderTypes)
            {
                // ReSharper disable ObjectCreationAsStatement
                switch (kvp.Value)
                {
                    case eExpanderType.SwampE8:
                        new SwampE8((uint)kvp.Key, device);
                        break;
                    case eExpanderType.SwampE4:
                        new SwampE4((uint)kvp.Key, device);
                        break;
                    case eExpanderType.Swe8:
                        new Swe8((uint)kvp.Key, device);
                        break;
                    case eExpanderType.None:
                        break;
                    default:
                        throw new InvalidOperationException(
                            string.Format("Unable to add expander of type {0} at address {1}", kvp.Value, kvp.Key));
                }
                // ReSharper restore ObjectCreationAsStatement
            }

            SetDevice(device);
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Override to apply properties to the settings instance.
        /// </summary>
        /// <param name="settings"></param>
        protected override void CopySettingsFinal(SwampAdapterSettings settings)
        {
            base.CopySettingsFinal(settings);

#if !NETSTANDARD

            settings.Ipid = m_Swamp == null ? default(byte) : (byte)m_Swamp.ID;

            settings.ClearExpanderTypes();
            
            if (m_Swamp == null)
                return;

            foreach (var expander in m_Swamp.Expanders)
                settings.SetExpanderType((int)expander.Number, expander.ExpanderType.ToIcd());
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Override to clear the instance settings.
        /// </summary>
        protected override void ClearSettingsFinal()
        {
            base.ClearSettingsFinal();

#if !NETSTANDARD
            SetDevice(null);
#endif
        }

        #endregion

        #region Console

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand command in GetBaseConsoleCommands())
                yield return command;
        }

        private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
        {
            return base.GetConsoleCommands();
        }

        #endregion
    }
}