using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Audio.Avr.Denon.Controls
{
    public sealed class DenonAvrSwitcherRoutingControl : AbstractRouteSwitcherControl<DenonAvrDevice>
	{
		private const string SELECT_INPUT = "SI";

		// Pulled from a couple models, almost certainly inconclusive
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, "TUNER"},
			{2, "DVD"},
			{3, "BD"},
			{4, "TV"},
			{5, "SAT/CBL"},
			{6, "MPLAY"},
			{7, "GAME"},
			{8, "AUX1"},
			{9, "NET"},
			{10, "PANDORA"},
			{11, "SIRIUSXM"},
			{12, "SPOTIFY"},
			{13, "FLICKR"},
			{14, "FAVORITES"},
			{15, "IRADIO"},
			{16, "SERVER"},
			{17, "USB/IPOD"},
			{18, "USB"},
			{19, "IPD"},
			{20, "IRP"},
			{21, "FVP"},
			{22, "PHONO"},
			{23, "CD"},
			{24, "HDP"},
			{25, "TV/CBL"},
			{26, "SAT"},
			{27, "VCR"},
			{28, "DVR"},
			{29, "V.AUX"},
			{30, "NET/USB"},
			{31, "XM"},
			{32, "IPOD"},
			{33, "GAME2"},
			{34, "DOCK"},
		};

		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
		/// </summary>
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Called when a route changes.
		/// </summary>
		public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

        private IRoutingGraph m_CachedRoutingGraph;

		private readonly SwitcherCache m_Cache;

		private readonly DenonAvrPowerControl m_PowerControl;

        private IRoutingGraph RoutingGraph
        {
            get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
        }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="powerControl"></param>
		public DenonAvrSwitcherRoutingControl(DenonAvrDevice parent, int id, DenonAvrPowerControl powerControl)
			: base(parent, id)
		{
			m_Cache = new SwitcherCache();
			Subscribe(m_Cache);

			m_PowerControl = powerControl;
			Subscribe(m_PowerControl);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			OnActiveTransmissionStateChanged = null;
			OnRouteChange = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Cache);
			Unsubscribe(m_PowerControl);
		}

		#region Methods

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return true;
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

			return new ConnectorInfo(input, eConnectionType.Audio | eConnectionType.Video);
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
		    return s_InputMap.ContainsKey(input);
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
		    return s_InputMap.Keys.Select(i => new ConnectorInfo(i, eConnectionType.Audio | eConnectionType.Video));
		}

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int address)
		{
			if (!ContainsOutput(address))
				throw new ArgumentOutOfRangeException("address");

			return new ConnectorInfo(address, eConnectionType.Audio | eConnectionType.Video);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return RoutingGraph.Connections.GetOutputConnection(new EndpointInfo(Parent.Id, Id, output)) != null;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return RoutingGraph.Connections
							   .GetOutputConnections(Parent.Id, Id)
							   .Select(c => new ConnectorInfo(c.Source.Address, eConnectionType.Audio | eConnectionType.Video));
		}

		/// <summary>
		/// Gets the outputs for the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
		{
			return m_Cache.GetOutputsForInput(input, type);
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
			return m_Cache.GetInputConnectorInfoForOutput(output, type);
		}

	    protected override InputPort CreateInputPort(ConnectorInfo input)
	    {
		    return new InputPort
		    {
			    Address = input.Address,
			    ConnectionType = input.ConnectionType,
			    InputId = GetInputId(input),
			    InputIdFeedbackSupported = true
		    };
	    }


	    protected override OutputPort CreateOutputPort(ConnectorInfo output)
	    {
		    bool supportsVideo = output.ConnectionType.HasFlag(eConnectionType.Video);
		    bool supportsAudio = output.ConnectionType.HasFlag(eConnectionType.Audio);
		    return new OutputPort
		    {
			    Address = output.Address,
			    ConnectionType = output.ConnectionType,
			    OutputId = "Denon Output",
			    OutputIdFeedbackSupport = true,
			    VideoOutputSource = supportsVideo ? GetActiveSourceIdName(output, eConnectionType.Video) : null,
			    VideoOutputSourceFeedbackSupport = supportsVideo,
			    AudioOutputSource = supportsAudio ? GetActiveSourceIdName(output, eConnectionType.Audio) : null,
			    AudioOutputSourceFeedbackSupport = supportsAudio
		    };
	    }

	    private string GetInputId(ConnectorInfo info)
	    {
		    return s_InputMap.GetValue(info.Address);
	    }

	    /// <summary>
		/// Performs the given route operation.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public override bool Route(RouteOperation info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			eConnectionType unsupported = info.ConnectionType & ~(eConnectionType.Audio | eConnectionType.Video);
			if (unsupported != eConnectionType.None)
				throw new ArgumentException("Unsupported connection type", "info");

			int input = info.LocalInput;
			return Route(input);
		}

		/// <summary>
		/// Routes the given input to the outputs.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private bool Route(int input)
		{
			if (!ContainsInput(input))
				throw new ArgumentOutOfRangeException("input");

			if (Parent.SetZonePowerWithRouting)
				m_PowerControl.PowerOn();

			string inputName = s_InputMap.GetValue(input);
			
			if (Parent.SetZonePowerWithRouting)
				m_PowerControl.PowerOn();

			DenonSerialData data = DenonSerialData.Command(SELECT_INPUT + inputName);
			Parent.SendData(data);

			return true;
		}

		/// <summary>
		/// Stops routing to the given output.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns>True if successfully cleared.</returns>
		public override bool ClearOutput(int output, eConnectionType type)
		{
			// If not SetZonePowerWithRouting, no way to power off
			if (!Parent.SetZonePowerWithRouting) 
				return false;
			
			// Power off on unroute
			m_PowerControl.PowerOff();
			return true;

			
		}
		
		/// <summary>
		/// Sets input for output on the routing cache for all outputs
		/// </summary>
		/// <param name="input"></param>
		private void SetCacheInputForOutput(int? input)
		{
			foreach (ConnectorInfo output in GetOutputs())
				m_Cache.SetInputForOutput(output.Address, input,
					eConnectionType.Audio | eConnectionType.Video);
		}

		/// <summary>
		/// Queries the receiver for the current input state
		/// </summary>
		private void QueryInput()
		{
			Parent.SendData(DenonSerialData.Request(SELECT_INPUT));
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(DenonAvrDevice parent)
		{
			base.Subscribe(parent);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
			parent.OnDataReceived += ParentOnOnDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(DenonAvrDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnInitializedChanged -= ParentOnOnInitializedChanged;
			parent.OnDataReceived -= ParentOnOnDataReceived;
		}

		/// <summary>
		/// Called when serial data is received from the device.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="response"></param>
		private void ParentOnOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			string data = response.GetCommand();

			if (data.StartsWith(SELECT_INPUT))
			{
				if (Parent.SetZonePowerWithRouting && m_PowerControl.PowerState != ePowerState.PowerOn)
					return;
				
				string inputName = data.Substring(SELECT_INPUT.Length);
				
				int input;
				if (s_InputMap.TryGetKey(inputName, out input) && ContainsInput(input))
					SetCacheInputForOutput(input);
				else
					SetCacheInputForOutput(null);
			}
		}

		/// <summary>
		/// Called when the parent initialization state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			QueryInput();
		}

		#endregion
		
		#region Power Control Callbacks
		
		private void Subscribe(DenonAvrPowerControl powerControl)
		{
			powerControl.OnPowerStateChanged += PowerControlOnOnPowerStateChanged;
		}
		
		private void Unsubscribe(DenonAvrPowerControl powerControl)
		{
			powerControl.OnPowerStateChanged -= PowerControlOnOnPowerStateChanged;
		}

		private void PowerControlOnOnPowerStateChanged(object sender, PowerDeviceControlPowerStateApiEventArgs e)
		{
			if (!Parent.SetZonePowerWithRouting)
				return;

			switch (e.Data.PowerState)
			{
				case ePowerState.PowerOff:
					SetCacheInputForOutput(null);
					break;
				case ePowerState.PowerOn:
					QueryInput();
					break;
			}
		}

		#endregion

		#region Cache Callbacks

		/// <summary>
		/// Subscribe to the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Subscribe(SwitcherCache cache)
		{
			cache.OnRouteChange += CacheOnOnRouteChange;
			cache.OnActiveInputsChanged += CacheOnOnActiveInputsChanged;
			cache.OnActiveTransmissionStateChanged += CacheOnOnActiveTransmissionStateChanged;
			cache.OnSourceDetectionStateChange += CacheOnOnSourceDetectionStateChange;
		}

		/// <summary>
		/// Unsubscribe from the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Unsubscribe(SwitcherCache cache)
		{
			cache.OnRouteChange -= CacheOnOnRouteChange;
			cache.OnActiveInputsChanged -= CacheOnOnActiveInputsChanged;
			cache.OnActiveTransmissionStateChanged -= CacheOnOnActiveTransmissionStateChanged;
			cache.OnSourceDetectionStateChange -= CacheOnOnSourceDetectionStateChange;
		}

		private void CacheOnOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(args));
		}

		private void CacheOnOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(args));
		}

		private void CacheOnOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs args)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(args));
		}

		private void CacheOnOnRouteChange(object sender, RouteChangeEventArgs args)
		{
			OnRouteChange.Raise(this, new RouteChangeEventArgs(args));
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
			
			yield return new ConsoleCommand("PrintAddressMap", "Prints a table of available input addresses",
				() => GetAvailableInputsTable());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}
		
		private string GetAvailableInputsTable()
		{
			TableBuilder table = new TableBuilder("Address", "Input Name");
			foreach (var kvp in s_InputMap) 
				table.AddRow(kvp.Key, kvp.Value);

			return table.ToString();
		}

		#endregion
	}
}
