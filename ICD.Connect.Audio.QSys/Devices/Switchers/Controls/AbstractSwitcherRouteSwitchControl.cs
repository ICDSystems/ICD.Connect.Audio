using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.EventArgs;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Audio.QSys.Devices.Switchers.Controls
{
	public abstract class AbstractSwitcherRouteSwitchControl<TSwitcherNamedComponentQSysDevice, TSwitcherNamedComponent> :
			AbstractRouteSwitcherControl<TSwitcherNamedComponentQSysDevice>
		where TSwitcherNamedComponentQSysDevice : ISwitcherNamedComponentQSysDevice<TSwitcherNamedComponent>
		where TSwitcherNamedComponent : class, ISwitcherNamedComponent
	{
		#region Fields

		private readonly RoutingGraphMidpointConnectionComponent m_MidpointComponent;

		private readonly SwitcherCache m_SwitcherCache;

		[CanBeNull]
		private TSwitcherNamedComponent m_SwitcherComponent;

		private readonly eConnectionType m_ConnectionMask;

		#endregion

		#region Events

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

		#endregion

		#region Properties

		private RoutingGraphMidpointConnectionComponent MidpointComponent { get { return m_MidpointComponent; } }

		protected SwitcherCache SwitcherCache { get { return m_SwitcherCache; } }

		protected TSwitcherNamedComponent SwitcherComponent
		{
			get { return m_SwitcherComponent; }
			private set
			{
				if (m_SwitcherComponent == value)
					return;

				Unsubscribe(m_SwitcherComponent);
				m_SwitcherComponent = value;
				Subscribe(m_SwitcherComponent);

				UpdateCurrentState();
			}
		}

		protected eConnectionType ConnectionMask { get { return m_ConnectionMask; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="connectionMask"></param>
		protected AbstractSwitcherRouteSwitchControl(TSwitcherNamedComponentQSysDevice parent, int id, eConnectionType connectionMask) :
			base(parent, id)
		{
			m_MidpointComponent = new RoutingGraphMidpointConnectionComponent(this, connectionMask);
			m_SwitcherCache = new SwitcherCache();
			m_ConnectionMask = connectionMask;
			SwitcherComponent = parent.NamedComponent;

			Subscribe(m_SwitcherCache);
			// ReSharper disable once VirtualMemberCallInConstructor
			Subscribe(m_SwitcherComponent);

			SetInputsDetected();
		}

		/// <summary>
		/// Sets all the inputs as detected
		/// </summary>
		private void SetInputsDetected()
		{
			foreach (var info in GetInputs())
				SwitcherCache.SetSourceDetectedState(info.Address, info.ConnectionType, true);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			OnRouteChange = null;
			OnActiveTransmissionStateChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_SwitcherComponent);
			Unsubscribe(m_SwitcherCache);
		}

		/// <summary>
		/// Performs the given route operation.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public override bool Route(RouteOperation info)
		{
			if (EnumUtils.HasFlags(ConnectionMask, info.ConnectionType))
				return SwitcherComponent.TrySetOutput(info.LocalOutput, info.LocalInput);
			
			throw new ArgumentOutOfRangeException("info",
				                                      string
					                                      .Format("Connection type of {0} is not in the supported connection types {1}",
					                                              info.ConnectionType, ConnectionMask));
		}

		/// <summary>
		/// Stops routing to the given output.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns>True if successfully cleared.</returns>
		public override bool ClearOutput(int output, eConnectionType type)
		{
			if (EnumUtils.HasFlags(ConnectionMask, type))
				return SwitcherComponent.TrySetOutput(output, null);

			throw new ArgumentOutOfRangeException("type",
			                                      string
				                                      .Format("Connection type of {0} is not in the supported connection types {1}",
				                                              type, ConnectionMask));
		}

		/// <summary>
		/// Update the current state for all outputs
		/// </summary>
		private void UpdateCurrentState()
		{
			if (SwitcherComponent == null)
				return;

			foreach (var output in GetOutputs())
			{
				UpdateCurrentState(output.Address);
			}
		}

		/// <summary>
		/// Update the current state for the given output
		/// </summary>
		/// <param name="output"></param>
		protected virtual void UpdateCurrentState(int output)
		{
			if (SwitcherComponent == null)
				return;

			var input = SwitcherComponent.TryGetOutputSelectState(output);
			if (input.HasValue)
				SwitcherCache.SetInputForOutput(output, input, ConnectionMask);
		}

		#region SwitcherCache Methods

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return SwitcherCache.GetSourceDetectedState(input, type);
		}

		/// <summary>
		/// Gets the outputs for the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
		{
			return SwitcherCache.GetOutputsForInput(input, type);
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
			return SwitcherCache.GetInputConnectorInfoForOutput(output, type);
		}

		#endregion

		#region MidpointComponent Methods

		/// <summary>
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			return MidpointComponent.GetInput(input);
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return MidpointComponent.ContainsInput(input);
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return MidpointComponent.GetInputs();
		}

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int address)
		{
			return MidpointComponent.GetOutput(address);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return MidpointComponent.ContainsOutput(output);
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return MidpointComponent.GetOutputs();
		}

		#endregion

		#region SwitcherCache Callbacks

		private void Subscribe(SwitcherCache switcherCache)
		{
			if (switcherCache == null)
				return;

			switcherCache.OnActiveInputsChanged += SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged += SwitcherCacheOnActiveTransmissionStateChanged;
			switcherCache.OnRouteChange += SwitcherCacheOnRouteChange;
			switcherCache.OnSourceDetectionStateChange += SwitcherCacheOnSourceDetectionStateChange;
		}

		private void Unsubscribe(SwitcherCache switcherCache)
		{
			if (switcherCache == null)
				return;

			switcherCache.OnActiveInputsChanged -= SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged -= SwitcherCacheOnActiveTransmissionStateChanged;
			switcherCache.OnRouteChange -= SwitcherCacheOnRouteChange;
			switcherCache.OnSourceDetectionStateChange -= SwitcherCacheOnSourceDetectionStateChange;
		}

		private void SwitcherCacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs args)
		{
			OnActiveInputsChanged.Raise(this, args);
		}

		private void SwitcherCacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
		{
			OnActiveTransmissionStateChanged.Raise(this, args);
		}

		private void SwitcherCacheOnRouteChange(object sender, RouteChangeEventArgs args)
		{
			OnRouteChange.Raise(this, args);
		}

		private void SwitcherCacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
		{
			OnSourceDetectionStateChange.Raise(this, args);
		}

		#endregion

		#region SwitcherComponent  Callbacks

		/// <summary>
		/// Subscribe to the switcher component
		/// </summary>
		/// <remarks>Called from AbstractRouteSwitchControl constructor</remarks>
		/// <param name="switcherComponent"></param>
		protected virtual void Subscribe(TSwitcherNamedComponent switcherComponent)
		{
			if (switcherComponent == null)
				return;

			switcherComponent.OnOutputSelectChanged += SwitcherComponentOnOnOutputSelectChanged;
		}

		/// <summary>
		/// Unsubscribe from the switcher component
		/// </summary>
		/// <param name="switcherComponent"></param>
		protected virtual void Unsubscribe(TSwitcherNamedComponent switcherComponent)
		{
			if (switcherComponent == null)
				return;

			switcherComponent.OnOutputSelectChanged -= SwitcherComponentOnOnOutputSelectChanged;
		}

		private void SwitcherComponentOnOnOutputSelectChanged(object sender, SwitcherOutputSelectChangedEventArgs args)
		{
			//Call UpdateCurrentState for the output so implemners can override as necessary
			UpdateCurrentState(args.Output);
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(TSwitcherNamedComponentQSysDevice parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			parent.OnNamedComponentChanged += ParentOnOnNamedComponentChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(TSwitcherNamedComponentQSysDevice parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			parent.OnNamedComponentChanged -= ParentOnOnNamedComponentChanged;
		}

		private void ParentOnOnNamedComponentChanged(object sender, System.EventArgs args)
		{
			SwitcherComponent = Parent.NamedComponent;
		}

		#endregion
	}

}
