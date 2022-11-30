using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;
#if !NETSTANDARD
using ICD.Common.Utils.Collections;
using Crestron.SimplSharpPro.AudioDistribution;
using Crestron.SimplSharpPro;
using ICD.Common.Properties;
using System.Linq;
using ICD.Common.Utils.EventArguments;
#endif

namespace ICD.Connect.Audio.CrestronPro.Swamp.Controls
{
    public sealed class SwampRouteSwitcherControl : AbstractRouteSwitcherControl<SwampAdapter>
    {
        private const int NUMBER_OF_INPUTS = 24;

        /// <summary>
        /// Switcher cache
        /// </summary>
        private readonly SwitcherCache m_SwitcherCache;

        /// <summary>
        /// Raised when an input source status changes.
        /// </summary>
        public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

        /// <summary>
        /// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
        /// </summary>
        public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

        /// <summary>
        /// Raised when a route changes.
        /// </summary>
        public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

        /// <summary>
        /// Raised when the device starts/stops actively transmitting on an output.
        /// </summary>
        public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

#if !NETSTANDARD

        private const int SPDIF_9_OUTPUT_ADDRESS = 9;
        private const int SPDIF_10_OUTPUT_ADDRESS = 10;

        /// <summary>
        /// Maps Krang output address to SPDIF collection index
        /// </summary>
        private static readonly BiDictionary<int, uint> s_SpdifAddressToCollectionIndex = new BiDictionary<int, uint>
        {
            { SPDIF_9_OUTPUT_ADDRESS, 9 },
            { SPDIF_10_OUTPUT_ADDRESS, 10 }
        };

        /// <summary>
        /// Maps Krang output address to Zone. Does not include SPDIF outputs, those need special handling
        /// </summary>
        private readonly BiDictionary<int, Zone> m_OutputAddressToZone;

        /// <summary>
        /// Swamp Device
        /// </summary>
        [CanBeNull]
        private Swamp24x8 m_Swamp;

        /// <summary>
        /// Swamp device
        /// </summary>
        [CanBeNull]
        private Swamp24x8 Swamp
        {
            get { return m_Swamp; }
            set
            {
                if (m_Swamp == value)
                    return;

                Unsubscribe(m_Swamp);
                m_Swamp = value;
                SetupSwampZoneDictionary(m_Swamp);
                Subscribe(m_Swamp);
            }
        }

#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        public SwampRouteSwitcherControl(SwampAdapter parent, int id) : base(parent, id)
        {
            m_SwitcherCache = new SwitcherCache();
            Subscribe(m_SwitcherCache);

#if !NETSTANDARD
            m_OutputAddressToZone = new BiDictionary<int, Zone>();
#endif
        }

        /// <summary>
        /// Override to release resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void DisposeFinal(bool disposing)
        {
            base.DisposeFinal(disposing);
            if (disposing)
            {
                Unsubscribe(m_SwitcherCache);
#if !NETSTANDARD
                Swamp = null;
#endif
            }
        }


        #region IRouteSwitchControl

        /// <summary>
        /// Returns true if a signal is detected at the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool GetSignalDetectedState(int input, eConnectionType type)
        {
            return m_SwitcherCache.GetSourceDetectedState(input, type);
        }

        /// <summary>
        /// Gets the input at the given address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override ConnectorInfo GetInput(int input)
        {
            if (!ContainsInput(input))
                throw new ArgumentOutOfRangeException("input");

            return new ConnectorInfo(input, eConnectionType.Audio);
        }

        /// <summary>
        /// Returns true if the destination contains an input at the given address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override bool ContainsInput(int input)
        {
            return input >= 1 && input <= NUMBER_OF_INPUTS;
        }

        /// <summary>
        /// Returns the inputs.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ConnectorInfo> GetInputs()
        {
            for (int i = 1; i <= NUMBER_OF_INPUTS; i++)
            {
                yield return GetInput(i);
            }
        }



        /// <summary>
        /// Gets the output at the given address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public override ConnectorInfo GetOutput(int address)
        {
            if (!ContainsOutput(address))
                throw new ArgumentOutOfRangeException("address", string.Format("No output at address {0}", address));

            return new ConnectorInfo(address, eConnectionType.Audio);
        }

        /// <summary>
        /// Returns true if the source contains an output at the given address.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public override bool ContainsOutput(int output)
        {
#if !NETSTANDARD
            if (output <= 0)
                throw new ArgumentOutOfRangeException("output");
            
            // Get the expander and zone numbers
            int expander = SwampAdapter.GetExpanderNumberForOutputAddress(output);
            int zone = SwampAdapter.GetZoneNumberForOutputAddress(output);

            // For non-expander zones, 1-10 are valid
            if (expander == 0)
                return zone >= 1 && zone <= 10;

            // Check the expander type zone count
            return zone > 0 && zone <= Parent.GetExpanderType(expander).GetZonesForExpander();
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Returns the outputs.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ConnectorInfo> GetOutputs()
        {
#if !NETSTANDARD
            Swamp24x8 swamp = Swamp;
            if (swamp == null)
                yield break;
            
            // Built-in Zones 1-10
            for (int i = 1; i <= 10; i++)
                yield return GetOutput(i);

            // Expander Zones
            foreach (var expander in swamp.Expanders)
            {
                foreach (var zone in expander.Zones)
                {
                    yield return GetOutput(
                        SwampAdapter.GetOutputAddressForExpanderZonePair(expander.Number, zone.Number));
                }
            }
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Gets the outputs for the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
        {
            return m_SwitcherCache.GetOutputsForInput(input, type);
        }

        /// <summary>
        /// Gets the input routed to the given output matching the given type.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Type has multiple flags.</exception>
        public override ConnectorInfo? GetInput(int output, eConnectionType type)
        {
            return m_SwitcherCache.GetInputConnectorInfoForOutput(output, type);
        }

        /// <summary>
        /// Performs the given route operation.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public override bool Route(RouteOperation info)
        {
#if !NETSTANDARD
            if (info == null)
                throw new ArgumentNullException("info");

            if (info.ConnectionType != eConnectionType.Audio)
                throw new ArgumentException("Unsupported connection type", "info");

            if (Swamp == null)
                return false;

            // Special Case Handling - spdif outputs
            uint spdifCollectionIndex;
            if (s_SpdifAddressToCollectionIndex.TryGetValue(info.LocalOutput, out spdifCollectionIndex))
            {
                Swamp.SpdifOuts[spdifCollectionIndex].SPDIFOutSource.UShortValue = (ushort)info.LocalInput;
                return true;
            }

            Zone outputZone;
            if (!m_OutputAddressToZone.TryGetValue(info.LocalOutput, out outputZone))
            {
                throw new ArgumentOutOfRangeException("info", string.Format("No output at address {0}", info.LocalOutput));
            }

            outputZone.Source.UShortValue = (ushort)info.LocalInput;
            return true;
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Stops routing to the given output.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="type"></param>
        /// <returns>True if successfully cleared.</returns>
        public override bool ClearOutput(int output, eConnectionType type)
        {
#if !NETSTANDARD
            if (type != eConnectionType.Audio)
                throw new ArgumentException("Unsupported connection type", "type");

            if (Swamp == null)
                return false;

            // Special Case Handling - spdif outputs
            uint spdifCollectionIndex;
            if (s_SpdifAddressToCollectionIndex.TryGetValue(output, out spdifCollectionIndex))
            {
                Swamp.SpdifOuts[spdifCollectionIndex].SPDIFOutSource.UShortValue = 0;
                return true;
            }

            Zone outputZone;
            if (!m_OutputAddressToZone.TryGetValue(output, out outputZone))
            {
                throw new ArgumentOutOfRangeException("output");
            }

            outputZone.Source.UShortValue = 0;
            return true;
#else
            throw new NotSupportedException();
#endif
        }

        protected override OutputPort CreateOutputPort(ConnectorInfo output)
        {
            int expander = SwampAdapter.GetExpanderNumberForOutputAddress(output.Address);
            int zone = SwampAdapter.GetZoneNumberForOutputAddress(output.Address);
            string outputId = expander == 0 ? string.Format("Main Unit, Zone {0}", zone) : string.Format("Expander {0}, Zone {1}", expander, zone);
            return new OutputPort
            {
                Address = output.Address,
                ConnectionType = output.ConnectionType,
                OutputId = outputId,
                OutputIdFeedbackSupport = true,
                VideoOutputSourceFeedbackSupport = false,
                AudioOutputSource = GetActiveSourceIdName(output, eConnectionType.Audio),
                AudioOutputSourceFeedbackSupport = true
            };
        }

        #endregion
        
        #region Parent Callbacks
#if !NETSTANDARD
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
        }
        
        private void ParentOnOnSwampChanged(object sender, GenericEventArgs<Swamp24x8> e)
        {
            Swamp = e.Data;
        }
#endif

        #endregion
        
        #region Swamp Callbacks
#if !NETSTANDARD



        private void SetupSwampZoneDictionary(Swamp24x8 swamp)
        {
            m_OutputAddressToZone.Clear();

            if (swamp == null)
                return;

            // Add built-in zones
            m_OutputAddressToZone.AddRange(swamp.Zones.Cast<Zone>(),
                z => SwampAdapter.GetOutputAddressForExpanderZonePair(0, z.Number));

            // Add zones for every expander
            foreach (Expander expander in swamp.Expanders)
            {
                foreach (Zone zone in expander.Zones)
                {
                    m_OutputAddressToZone.Add(
                        SwampAdapter.GetOutputAddressForExpanderZonePair(expander.Number, zone.Number), zone);

                }
            }
        }
        private void Subscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.ZoneChangeEvent += SwampOnZoneChangeEvent;
            swamp.BaseEvent += SwampOnBaseEvent;

            foreach (Expander expander in swamp.Expanders) 
                expander.ZoneChangeEvent += SwampOnZoneChangeEvent;
        }
        
        private void Unsubscribe(Swamp24x8 swamp)
        {
            if (swamp == null)
                return;

            swamp.ZoneChangeEvent -= SwampOnZoneChangeEvent;
            swamp.BaseEvent -= SwampOnBaseEvent;
            foreach (Expander expander in swamp.Expanders) 
                expander.ZoneChangeEvent -= SwampOnZoneChangeEvent;
        }

        private void SwampOnBaseEvent(GenericBase device, BaseEventArgs args)
        {
            switch (args.EventId)
            {
                case Crestron.SimplSharpPro.AudioDistribution.Swamp.SPDIFOut9SourceFeedbackEventId:
                case Crestron.SimplSharpPro.AudioDistribution.Swamp.SPDIFOut10SourceFeedbackEventId:
                    UpdateSpdifSourceFeedback();
                    break;
            }
        }

        private void UpdateSpdifSourceFeedback()
        {
            if (Swamp == null)
            {
                foreach(int outputAddress in s_SpdifAddressToCollectionIndex.Keys)
                    m_SwitcherCache.SetInputForOutput(outputAddress, null, eConnectionType.Audio);
                return;
            }

            foreach (var kvp in s_SpdifAddressToCollectionIndex)
            {
                m_SwitcherCache.SetInputForOutput(kvp.Key,
                    SwampSourceFeedbackToKrang(Swamp.SpdifOuts[kvp.Value].SPDIFOutSourceFeedback.UShortValue),
                    eConnectionType.Audio);
            }
        }

        private void SwampOnZoneChangeEvent(object sender, ZoneEventArgs args)
        {
            switch (args.EventId)
            {
                case ZoneEventIds.SourceFeedbackEventId:
                    SwampZoneHandleSourceChanged(args.Zone);
                    break;
            }
        }

        private void SwampZoneHandleSourceChanged(Zone zone)
        {
            int outputAddress;
            if (!m_OutputAddressToZone.TryGetKey(zone, out outputAddress))
                return;

            ushort sourceFeedback = zone.SourceFeedback.UShortValue;

            m_SwitcherCache.SetInputForOutput(outputAddress, SwampSourceFeedbackToKrang(sourceFeedback), eConnectionType.Audio);

        }

        
        
#endif

        #endregion

        #region SwitcherCacheCallbacks
        private void Subscribe(SwitcherCache switcherCache)
        {
            switcherCache.OnRouteChange += SwitcherCacheOnOnRouteChange;
            switcherCache.OnActiveInputsChanged += SwitcherCacheOnOnActiveInputsChanged;
            switcherCache.OnActiveTransmissionStateChanged += SwitcherCacheOnOnActiveTransmissionStateChanged;
            switcherCache.OnSourceDetectionStateChange += SwitcherCacheOnOnSourceDetectionStateChange;
        }

        private void Unsubscribe(SwitcherCache switcherCache)
        {
            switcherCache.OnRouteChange -= SwitcherCacheOnOnRouteChange;
            switcherCache.OnActiveInputsChanged -= SwitcherCacheOnOnActiveInputsChanged;
            switcherCache.OnActiveTransmissionStateChanged -= SwitcherCacheOnOnActiveTransmissionStateChanged;
            switcherCache.OnSourceDetectionStateChange -= SwitcherCacheOnOnSourceDetectionStateChange;
        }

        private void SwitcherCacheOnOnRouteChange(object sender, RouteChangeEventArgs e)
        {
            OnRouteChange.Raise(this, new RouteChangeEventArgs(e));
        }
        private void SwitcherCacheOnOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs e)
        {
            OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(e));
        }
        private void SwitcherCacheOnOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs e)
        {
            OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(e));
        }
        private void SwitcherCacheOnOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs e)
        {
            OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(e));
        }

        #endregion

        /// <summary>
        /// Converts Crestron Source Feedback to Krang Source Feedback
        /// uint to int?, and 0 is converted to null
        /// </summary>
        /// <param name="sourceFeedback"></param>
        /// <returns></returns>
        private static int? SwampSourceFeedbackToKrang(uint sourceFeedback)
        {
            return sourceFeedback == 0 ? null : (int?)sourceFeedback;
        }
    }
}