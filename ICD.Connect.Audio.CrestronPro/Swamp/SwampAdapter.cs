using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.Audio.CrestronPro.Swamp.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Settings;
#if !NETSTANDARD
using ICD.Common.Properties;
using ICD.Connect.Misc.CrestronPro;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Misc.CrestronPro.Utils;
using Crestron.SimplSharpPro.AudioDistribution;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.GeneralIO;
using ICD.Common.Utils.EventArguments;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp
{
    public sealed class SwampAdapter : AbstractDevice<SwampAdapterSettings>
    {

        /// <summary>
        /// What output address offset is for each expander
        /// </summary>
        private const int EXPANDER_OUTPUT_ADDRESS_OFFSET = 10;

#if !NETSTANDARD
        public event EventHandler<GenericEventArgs<Swamp24x8>> OnSwampChanged;

        [CanBeNull] 
        private Swamp24x8 m_Swamp;
        
        [CanBeNull]
        public Swamp24x8 Swamp
        {
            get { return m_Swamp; }
        }
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
            
            OnSwampChanged.Raise(this, device);
        }

        public eExpanderType GetExpanderType(int expanderNumber)
        {
            if (m_Swamp == null)
                return eExpanderType.None;
            
            Expander expander;
            if (!m_Swamp.Expanders.TryGetValue((uint)expanderNumber, out expander))
                return eExpanderType.None;

            return expander.ExpanderType.ToIcd();
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

        /// <summary>
        /// Override to add controls to the device.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="addControl"></param>
        protected override void AddControls(SwampAdapterSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
        {
            base.AddControls(settings, factory, addControl);
            #if !NETSTANDARD

            addControl(new SwampRouteSwitcherControl(this, 0));

            // Add built-in zone controls
            for (int i = 1; i <= 8; i++)
            {
                addControl(new BuiltInZoneSwampVolumeControl(this, i, i));
            }
            
            // Loop over expanders
            foreach (var kvp in settings.ExpanderTypes)
            {
                // Loop over zones
                for (int i = 1; i <= kvp.Value.GetZonesForExpander(); i++)
                {
                    int controlId = (kvp.Key * 10) + i;
                    addControl(new ExpanderZoneSwampVolumeControl(this, controlId, kvp.Key, i));
                }
            }

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
        
        #region Static Methods

        /// <summary>
        /// Gets the expander number for the given output address
        /// Returns 0 for outputs on the main SWAMP unit
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int GetExpanderNumberForOutputAddress(int output)
        {
            // Special case - zone 10 on the main unit
            if (output == 10)
                return 0;
            
            // General case
            return output / EXPANDER_OUTPUT_ADDRESS_OFFSET;
        }

        /// <summary>
        /// Gets the zone number on the expander (or main SWAMP unit)
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int GetZoneNumberForOutputAddress(int output)
        {
            // Special case - Zone 10 on the main unit
            if (output == 10)
                return 10;

            // General case
            return output % EXPANDER_OUTPUT_ADDRESS_OFFSET;
        }

        public static int GetOutputAddressForExpanderZonePair(uint expander, uint zone)
        {
            return GetOutputAddressForExpanderZonePair((int)expander, (int)zone);
        }
        
        /// <summary>
        /// Get the output number for a given expander and zone
        /// </summary>
        /// <param name="expander"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static int GetOutputAddressForExpanderZonePair(int expander, int zone)
        {
            // Expander number * offset + zone number
            return (expander * EXPANDER_OUTPUT_ADDRESS_OFFSET) + zone;
        }
        
        #endregion
    }
}