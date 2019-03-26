using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Audio.Devices
{
	/// <summary>
	/// Very similar to the mock switcher control. Fakes an audio switch with a single output.
	/// </summary>
	public sealed class GenericAmpRouteSwitcherControl : AbstractRouteSwitcherControl<GenericAmpDevice>, IRouteInputSelectControl
	{
		/// <summary>
		/// Called when a route changes.
		/// </summary>
		public override event EventHandler<RouteChangeEventArgs> OnRouteChange;

		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
		/// </summary>
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

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
		public GenericAmpRouteSwitcherControl(GenericAmpDevice parent, int id)
			: base(parent, id)
		{
			m_Cache = new SwitcherCache();
			Subscribe(m_Cache);
			Parent.OnVolumeChanged += ParentOnVolumeChanged;
			Parent.OnMuteChanged += ParentOnMuteChanged;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnRouteChange = null;
			OnActiveInputsChanged = null;
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			Parent.OnVolumeChanged -= ParentOnVolumeChanged;
			Parent.OnMuteChanged -= ParentOnMuteChanged;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Cache);
		}

		#region Methods

		public override IEnumerable<InputPort> GetInputPorts()
		{
			foreach (ConnectorInfo input in GetInputs().Where(input => input.ConnectionType.HasFlag(eConnectionType.Audio)))
			{
				yield return new InputPort
				{
					Address = input.Address,
					ConnectionType = input.ConnectionType,
					InputId = string.Format("Audio Input {0}", input.Address),
					InputIdFeedbackSupported = true,
					InputName = GetInputName(input),
					InputNameFeedbackSupported = GetInputName(input) != null 
				};
			}
		}

		public override IEnumerable<OutputPort> GetOutputPorts()
		{
			foreach (ConnectorInfo output in GetOutputs().Where(output => output.ConnectionType.HasFlag(eConnectionType.Audio)))
			{
				yield return new OutputPort
				{
					Address = output.Address,
					ConnectionType = output.ConnectionType,
					OutputId = string.Format("Audio Output {0}", output.Address),
					OutputIdFeedbackSupport = true,
					AudioOutputVolume = Parent.GetVolumeState(),
					AudioOutputMuteFeedbackSupported = true,
					AudioOutputMute = Parent.GetMuteState(),
					AudioOutputSource = GetActiveSourceIdName(output, eConnectionType.Audio),
					AudioOutputSourceFeedbackSupport = true
				};
			}
		}

		/// <summary>
		/// Routes the input to the given output.
		/// </summary>
		/// <param name="info"></param>
		public override bool Route(RouteOperation info)
		{
			if (info.ConnectionType != eConnectionType.Audio)
				throw new NotSupportedException(string.Format("Can only route {0}", eConnectionType.Audio));

			if (info.LocalOutput != 1)
				throw new NotSupportedException(string.Format("Invalid output address {0}", info.LocalOutput));

			return SetActiveInput(info.LocalInput);
		}

		/// <summary>
		/// Stops routing to the given output.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		public override bool ClearOutput(int output, eConnectionType type)
		{
			if (!ContainsOutput(output))
				throw new NotSupportedException(string.Format("Invalid output address {0}", output));

			return m_Cache.SetInputForOutput(output, null, type);
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

			return new ConnectorInfo(address, eConnectionType.Audio);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return output == 1;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			yield return new ConnectorInfo(1, eConnectionType.Audio);
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
			Connection connection = RoutingGraph.Connections.GetInputConnection(this, input);
			return connection != null && connection.ConnectionType.HasFlag(eConnectionType.Audio);
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return
				RoutingGraph.Connections
							.GetInputConnections(Parent.Id, Id, eConnectionType.Audio)
							.Select(c => new ConnectorInfo(c.Destination.Address, eConnectionType.Audio));
		}

		/// <summary>
		/// Returns true if video is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return m_Cache.GetSourceDetectedState(input, type);
		}

		/// <summary>
		/// Gets the current active input.
		/// </summary>
		public int? GetActiveInput(eConnectionType flag)
		{
			if (flag != eConnectionType.Audio)
				return null;

			ConnectorInfo? input = GetInput(1, eConnectionType.Audio);
			return input.HasValue ? input.Value.Address : (int?)null;
		}

		/// <summary>
		/// Sets the current active input.
		/// </summary>
		/// <param name="input"></param>
		public bool SetActiveInput(int? input)
		{
			if (input == null || !ContainsInput((int)input))
				throw new NotSupportedException(string.Format("Invalid input address {0}", input));

			return m_Cache.SetInputForOutput(1, input, eConnectionType.Audio);
		}

		/// <summary>
		/// Sets the current active input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		void IRouteInputSelectControl.SetActiveInput(int? input, eConnectionType type)
		{
			if (type.HasFlag(eConnectionType.Audio))
				SetActiveInput(input);
		}

		private string GetInputName(ConnectorInfo info)
		{
			IVolumePoint vp = Parent.GetVolumePointForInput(info.Address);
			return vp != null ? vp.Name : null;
		}

		#endregion

		#region Cache Callbacks

		/// <summary>
		/// Subscribe to the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Subscribe(SwitcherCache cache)
		{
			cache.OnRouteChange += CacheOnRouteChange;
			cache.OnActiveInputsChanged += CacheOnActiveInputsChanged;
			cache.OnSourceDetectionStateChange += CacheOnSourceDetectionStateChange;
			cache.OnActiveTransmissionStateChanged += CacheOnActiveTransmissionStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the cache events.
		/// </summary>
		/// <param name="cache"></param>
		private void Unsubscribe(SwitcherCache cache)
		{
			cache.OnRouteChange -= CacheOnRouteChange;
			cache.OnActiveInputsChanged -= CacheOnActiveInputsChanged;
			cache.OnSourceDetectionStateChange -= CacheOnSourceDetectionStateChange;
			cache.OnActiveTransmissionStateChanged -= CacheOnActiveTransmissionStateChanged;
		}

		private void CacheOnRouteChange(object sender, RouteChangeEventArgs args)
		{
			OnRouteChange.Raise(this, new RouteChangeEventArgs(args));
			OutputPort outputPort = GetOutputPort(args.Output);
			ConnectorInfo info = GetOutput(args.Output);
			if (args.Type.HasFlag(eConnectionType.Video))
				outputPort.VideoOutputSource = GetActiveSourceIdName(info, eConnectionType.Video);
			if (args.Type.HasFlag(eConnectionType.Audio))
				outputPort.AudioOutputSource = GetActiveSourceIdName(info, eConnectionType.Audio);
		}

		private void CacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(args));
		}

		private void CacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(args));
		}

		private void CacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs args)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(args));
		}

		#endregion

		#region Parent Callbacks

		private void ParentOnMuteChanged(object sender, BoolEventArgs args)
		{
			// this device should always have only one output, 
			//so firstordefault is easier than looking up an index which is always 0 anyway
			OutputPort output = GetOutputPort(1);
			output.AudioOutputMute = args.Data;
		}

		private void ParentOnVolumeChanged(object sender, FloatEventArgs args)
		{
			// this device should always have only one output, 
			//so firstordefault is easier than looking up an index which is always 0 anyway
			OutputPort output = GetOutputPort(1);
			output.AudioOutputVolume = args.Data;
		}

		#endregion
	}
}
