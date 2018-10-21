using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Audio.Denon.Devices;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Audio.Denon.Controls
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
			{32, "IPOD"}
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
		/// Gets the routing graph.
		/// </summary>
		public IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DenonAvrSwitcherRoutingControl(DenonAvrDevice parent, int id)
			: base(parent, id)
		{
			m_Cache = new SwitcherCache();
			Subscribe(m_Cache);

			Subscribe(parent);
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
			Unsubscribe(Parent);
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
			if (!s_InputMap.ContainsKey(input))
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
			Connection connection = RoutingGraph.Connections.GetOutputConnection(new EndpointInfo(Parent.Id, Id, address));
			if (connection == null)
				throw new ArgumentOutOfRangeException("address");

			return new ConnectorInfo(connection.Source.Address, connection.ConnectionType);
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
							   .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		/// <summary>
		/// Gets the outputs for the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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

			throw new NotImplementedException();
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

		#region Parent Callbacks

		private void Subscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
			parent.OnDataReceived += ParentOnOnDataReceived;
		}

		private void Unsubscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged -= ParentOnOnInitializedChanged;
			parent.OnDataReceived -= ParentOnOnDataReceived;
		}

		private void ParentOnOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			string data = response.GetCommand();

			switch (data)
			{
				case SELECT_INPUT:
					return;
			}
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			Parent.SendData(DenonSerialData.Request(SELECT_INPUT));
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
	}
}
