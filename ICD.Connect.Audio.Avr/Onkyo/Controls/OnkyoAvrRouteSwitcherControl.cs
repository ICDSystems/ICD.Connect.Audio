using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Audio.Avr.Onkyo.Controls
{
	/// <summary>
	/// Switcher control for Onkyo AVR's
	/// Supports number of zones specified by Parent's Zone property,
	/// and based on routing graph connections.
	/// TODO: Have routing/derouting zones (at least secondary zones) control the power control also
	/// </summary>
	public sealed class OnkyoAvrRouteSwitcherControl : AbstractRouteSwitcherControl<IOnkyoAvrDevice>
	{
		private const int PARAMETER_INPUT_MIRROR = 0x80;
		private const int MAIN_OUTPUT = 1;
		
		/// <summary>
		/// Maps Krang addresses to Onkyo parameter input numbers
		/// Parameter input numbers are represented in hex to match representations in the Onkyo
		/// Key: Krang Address
		/// Value: Onkyo Parameter
		/// </summary>
		private static readonly BiDictionary<int, int> s_InputAddressToParameter = new BiDictionary<int, int>
		{
			{ 01, 0x00 },
			{ 02, 0x01 },
			{ 03, 0x02 },
			{ 04, 0x03 },
			{ 05, 0x04 },
			{ 06, 0x05 },
			{ 07, 0x06 },
			{ 08, 0x07 },
			{ 09, 0x08 },
			{ 10, 0x09 },
			{ 11, 0x10 },
			{ 12, 0x11 },
			{ 13, 0x12 },
			{ 14, 0x20 },
			{ 15, 0x21 },
			{ 16, 0x22 },
			{ 17, 0x23 },
			{ 18, 0x24 },
			{ 19, 0x25 },
			{ 20, 0x26 },
			{ 21, 0x27 },
			{ 22, 0x28 },
			{ 23, 0x29 },
			{ 24, 0x2A },
			{ 25, 0x2B },
			{ 26, 0x2C },
			{ 27, 0x2D },
			{ 28, 0x2E },
			{ 29, 0x2F },
			{ 30, 0x30 },
			{ 31, 0x31 },
			{ 32, 0x32 },
			{ 33, 0x33 },
			{ 34, 0x40 },
			{ 35, 0x41 },
			{ 36, 0x42 },
			{ 37, 0x44 },
			{ 38, 0x45 },
			{ 39, 0x55 },
			{ 40, 0x56 },
			{ 41, 0x57 }
			// Special Cas handled elsewhere: 0x80 - follow main zone
		};

		/// <summary>
		/// Maps Krang addresses to logical input names
		/// May not be correct for all receivers
		/// </summary>
		private static readonly Dictionary<int, string> s_InputAddressToLogicalName = new Dictionary<int, string>
		{
			{ 01, "STB/DVR" },
			{ 02, "CBL/SAT" },
			{ 03, "Game/TV" },
			{ 04, "AUX1" },
			{ 05, "AUX2" },
			{ 06, "PC" },
			{ 07, "Video 7" },
			{ 08, "Extra 1" },
			{ 09, "Extra 2" },
			{ 10, "Extra 3" },
			{ 11, "BD/DVD" },
			{ 12, "STRM Box" },
			{ 13, "TV" },
			{ 14, "TAPE/TV" },
			{ 15, "TAPE2" },
			{ 16, "Phono" },
			{ 17, "CD/TV" },
			{ 18, "FM" },
			{ 19, "AM" },
			{ 20, "Tuner" },
			{ 21, "Music Server" },
			{ 22, "Internet Radio" },
			{ 23, "USB" },
			{ 24, "USB(Rear)" },
			{ 25, "Network" },
			{ 26, "USB(Toggle)" },
			{ 27, "Airplay" },
			{ 28, "Bluetooth" },
			{ 29, "USB DAC In" },
			{ 30, "Multi-Channel" },
			{ 31, "XM" },
			{ 32, "Sirius" },
			{ 33, "DAB" },
			{ 34, "Universal Port" },
			{ 35, "Line" },
			{ 36, "Line2" },
			{ 37, "Optical" },
			{ 38, "Coaxial" },
			{ 39, "HDMI 5" },
			{ 40, "HDMI 6" },
			{ 41, "HDMI 7" }
		};

		private static readonly BiDictionary<int, eOnkyoCommand> s_OutputAddressToOnkyoCommand =
			new BiDictionary<int, eOnkyoCommand>
			{
				{ 1, eOnkyoCommand.Input },
				{ 2, eOnkyoCommand.Zone2Input },
				{3, eOnkyoCommand.Zone3Input}
			};

		private static readonly Dictionary<int, string> s_OutputAddressToLogicalName = new Dictionary<int, string>
		{
			{ 1, "Main Output" },
			{ 2, "Zone 2 Output" },
			{ 3, "Zone 3 Output" }
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

		private readonly SwitcherCache m_Cache;

		private IRoutingGraph m_CachedRoutingGraph;

		/// <summary>
		/// A collection of outputs that are set to mirror the main output (0x80)
		/// Contains the output addresses of those outputs, so they will be updated
		/// when the main zone output changes.
		/// </summary>
		private readonly IcdHashSet<int> m_MirrorMainOutputs;

		private int Zones
		{
			get { return Parent.Zones; }
		}
		
		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		private IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public OnkyoAvrRouteSwitcherControl(IOnkyoAvrDevice parent, int id)
			: base(parent, id)
		{
			m_MirrorMainOutputs = new IcdHashSet<int>();
			m_Cache = new SwitcherCache();
			Subscribe(m_Cache);
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
			return RoutingGraph.Connections.GetInputConnection(new EndpointInfo(Parent.Id, Id, input)) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return RoutingGraph.Connections
			                   .GetInputConnections(Parent.Id, Id)
			                   .Where(c => s_InputAddressToParameter.ContainsKey(c.Destination.Address))
			                   .Select(c => new ConnectorInfo(c.Destination.Address,
				                   eConnectionType.Audio | eConnectionType.Video));
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
			                   .Where(c => s_OutputAddressToOnkyoCommand.ContainsKey(c.Source.Address))
			                   .Where(c => c.Source.Address <= Zones)
			                   .Select(c =>
				                   new ConnectorInfo(c.Source.Address, eConnectionType.Audio | eConnectionType.Video));
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
				InputId = s_InputAddressToLogicalName[input.Address],
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
				OutputId = s_OutputAddressToLogicalName[output.Address],
				OutputIdFeedbackSupport = true,
				VideoOutputSource = supportsVideo ? GetActiveSourceIdName(output, eConnectionType.Video) : null,
				VideoOutputSourceFeedbackSupport = supportsVideo,
				AudioOutputSource = supportsAudio ? GetActiveSourceIdName(output, eConnectionType.Audio) : null,
				AudioOutputSourceFeedbackSupport = supportsAudio
			};
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
			int output = info.LocalOutput;
			return Route(input, output);
		}

		/// <summary>
		/// Routes the given input to the outputs.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		private bool Route(int input, int output)
		{
			if (!s_InputAddressToParameter.ContainsKey(input))
				throw new ArgumentOutOfRangeException("input");

			if (!s_OutputAddressToOnkyoCommand.ContainsKey(output))
				throw new ArgumentOutOfRangeException("output");

			int inputParameter;
			if (!s_InputAddressToParameter.TryGetValue(input, out inputParameter))
				return false;

			eOnkyoCommand outputCommand;
			if (!s_OutputAddressToOnkyoCommand.TryGetValue(output, out outputCommand))
				return false;

			Parent.SendCommand(OnkyoIscpCommand.GetSetCommand(outputCommand, inputParameter));

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
			// No way of clearing output
			return false;
		}

		#endregion

		#region Private Methods

		private void QueryInputs()
		{
			foreach (ConnectorInfo output in GetOutputs())
			{
				eOnkyoCommand outputCommand = s_OutputAddressToOnkyoCommand.GetValue(output.Address);
				Parent.SendCommand(OnkyoIscpCommand.GetQueryCommand(outputCommand));
			}
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(IOnkyoAvrDevice parent)
		{
			base.Subscribe(parent);

			foreach(eOnkyoCommand command in s_OutputAddressToOnkyoCommand.Values)
				parent.RegisterCommandCallback(command, InputResponseCallback);
			
			parent.OnIsOnlineStateChanged += ParentOnOnIsOnlineStateChanged;

			if (parent.IsOnline)
				QueryInputs();
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IOnkyoAvrDevice parent)
		{
			base.Unsubscribe(parent);

			parent.UnregisterCommandCallback(eOnkyoCommand.Input, InputResponseCallback);
			parent.OnIsOnlineStateChanged -= ParentOnOnIsOnlineStateChanged;
		}

		private void InputResponseCallback(eOnkyoCommand responseCommand, string responseParameter,
		                                   ISerialData sentData)
		{
			if (string.Equals(responseParameter, OnkyoIscpCommand.ERROR_PARAMETER))
			{
				// Not all receivers support all zones, so expect to get some N/A responses for zones that
				// don't exist on a particular model.
				string sentCommand = sentData == null ? "[Unknown Command]" : sentData.Serialize();
				Logger.Log(eSeverity.Debug, "N/A Response to command {0}", sentCommand);
				return;
			}

			int outputAddress;
			if (!s_OutputAddressToOnkyoCommand.TryGetKey(responseCommand, out outputAddress))
				return; //Should never get here??

			int inputParameter = StringUtils.FromIpIdString(responseParameter);
			if (inputParameter == PARAMETER_INPUT_MIRROR)
			{
				//Add to m_MirrorMainsOutputs collection, set output to the main output, and don't continue
				m_MirrorMainOutputs.Add(outputAddress);
				int? mainInput = m_Cache.GetInputForOutput(MAIN_OUTPUT, eConnectionType.Video);
				m_Cache.SetInputForOutput(outputAddress, mainInput, eConnectionType.Audio | eConnectionType.Video);
				return;
			}
			
			int inputAddress;
			int? inputAddressToSet;

			// If we can't find the address for corresponding parameter, set the outputs to null
			if (!s_InputAddressToParameter.TryGetKey(inputParameter, out inputAddress))
			{
				inputAddressToSet = null;
				Logger.Log(eSeverity.Warning, "No input address found for parameter {0:X2}", inputParameter);
			}
			else
				inputAddressToSet = inputAddress;
			
			// This output is no longer set to mirror main
			if (outputAddress != MAIN_OUTPUT)
				m_MirrorMainOutputs.Remove(outputAddress);

			// Set the switcher cache
			m_Cache.SetInputForOutput(outputAddress, inputAddressToSet,
				eConnectionType.Audio | eConnectionType.Video);
			
			// If this is the main output, update mirrors
			if (outputAddress == MAIN_OUTPUT)
			{
				foreach (var mirroredOutput in m_MirrorMainOutputs)
					m_Cache.SetInputForOutput(mirroredOutput, inputAddressToSet,
						eConnectionType.Audio | eConnectionType.Video);
			}
		}

		private void ParentOnOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			if (args.Data)
				QueryInputs();
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

			yield return new GenericConsoleCommand<int>("SetOutputToMirror",
				"Sets the specificed output to mirror the main zone", (o) => SetOutputToMirror(o));

			yield return new ConsoleCommand("PrintAddressMap", "Prints a table of available input addresses",
				() => GetAvailableInputsTable());

		}

		private void SetOutputToMirror(int output)
		{
			eOnkyoCommand command;
			if (!s_OutputAddressToOnkyoCommand.TryGetValue(output, out command))
				return;
			
			Parent.SendCommand(OnkyoIscpCommand.GetSetCommand(command, PARAMETER_INPUT_MIRROR));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string GetAvailableInputsTable()
		{
			TableBuilder table = new TableBuilder("Address", "Input Name", "Onkyo Parameter");
			foreach (var kvp in s_InputAddressToParameter) 
				table.AddRow(kvp.Key, s_InputAddressToLogicalName[kvp.Key], string.Format("{0:X2}", kvp.Value));

			return table.ToString();
		}

	#endregion
	}
}