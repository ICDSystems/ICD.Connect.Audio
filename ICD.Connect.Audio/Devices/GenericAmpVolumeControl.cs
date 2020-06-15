using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Audio.Devices
{
	public sealed class GenericAmpVolumeControl : AbstractVolumeDeviceControl<GenericAmpDevice>
	{
		[CanBeNull]
		private IVolumeDeviceControl m_ActiveControl;

		#region Properties

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return m_ActiveControl == null ? 0 : m_ActiveControl.VolumeLevelMin; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return m_ActiveControl == null ? 0 : m_ActiveControl.VolumeLevelMax; } }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		public override string VolumeString { get { return m_ActiveControl == null ? base.VolumeString : m_ActiveControl.VolumeString; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public GenericAmpVolumeControl(GenericAmpDevice parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			ExecuteCallbackForActiveControl(c => c.SetIsMuted(mute), eVolumeFeatures.MuteAssignment);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			ExecuteCallbackForActiveControl(c => c.ToggleIsMuted(), eVolumeFeatures.Mute);
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			ExecuteCallbackForActiveControl(c => c.SetVolumeLevel(level), eVolumeFeatures.VolumeAssignment);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			ExecuteCallbackForActiveControl(c => c.VolumeIncrement(), eVolumeFeatures.Volume);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			ExecuteCallbackForActiveControl(c => c.VolumeDecrement(), eVolumeFeatures.Volume);
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			ExecuteCallbackForActiveControl(c => c.VolumeRamp(increment, timeout), eVolumeFeatures.VolumeRamp);
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			ExecuteCallbackForActiveControl(c => c.VolumeRampStop(), eVolumeFeatures.VolumeRamp);
		}

		#endregion

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(Parent);

			base.DisposeFinal(disposing);
		}

		#region Private Methods

		/// <summary>
		/// Sets the currently wrapped volume control.
		/// </summary>
		/// <param name="volumeControl"></param>
		private void SetActiveControl(IVolumeDeviceControl volumeControl)
		{
			if (volumeControl == m_ActiveControl)
				return;

			// Mute the old active device
			if (SupportedVolumeFeatures.HasFlag(eVolumeFeatures.MuteAssignment))
				SetIsMuted(true);

			Unsubscribe(m_ActiveControl);
			m_ActiveControl = volumeControl;
			Subscribe(m_ActiveControl);

			// Unmute the next active device
			if (SupportedVolumeFeatures.HasFlag(eVolumeFeatures.MuteAssignment))
				SetIsMuted(false);

			UpdateState();
		}

		/// <summary>
		/// Validates the feature is supported before running the callback.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="features"></param>
		private void ExecuteCallbackForActiveControl(Action<IVolumeDeviceControl> callback, eVolumeFeatures features)
		{
			eVolumeFeatures unsupported = features.ExcludeFlags(SupportedVolumeFeatures);
			if (unsupported != eVolumeFeatures.None)
				throw new NotSupportedException(string.Format("{0} is unsupported", unsupported));

			callback(m_ActiveControl);
		}

		/// <summary>
		/// Updates the state of this control to match the wrapped volume control.
		/// </summary>
		private void UpdateState()
		{
			VolumeLevel = m_ActiveControl == null ? 0 : m_ActiveControl.VolumeLevel;
			IsMuted = m_ActiveControl != null && m_ActiveControl.IsMuted;
			SupportedVolumeFeatures = m_ActiveControl == null ? eVolumeFeatures.None : m_ActiveControl.SupportedVolumeFeatures;
		}

		#endregion

		#region Volume Control Callbacks

		/// <summary>
		/// Subscribe to the volume device control callbacks.
		/// </summary>
		/// <param name="activeControl"></param>
		private void Subscribe(IVolumeDeviceControl activeControl)
		{
			if (activeControl == null)
				return;

			activeControl.OnIsMutedChanged += ActiveControlOnIsMutedChanged;
			activeControl.OnVolumeChanged += ActiveControlOnVolumeChanged;
			activeControl.OnSupportedVolumeFeaturesChanged += ActiveControlOnSupportedVolumeFeaturesChanged;
		}

		/// <summary>
		/// Unsubscribe from the volume device control callbacks.
		/// </summary>
		/// <param name="activeControl"></param>
		private void Unsubscribe(IVolumeDeviceControl activeControl)
		{
			if (activeControl == null)
				return;

			activeControl.OnIsMutedChanged -= ActiveControlOnIsMutedChanged;
			activeControl.OnVolumeChanged -= ActiveControlOnVolumeChanged;
			activeControl.OnSupportedVolumeFeaturesChanged -= ActiveControlOnSupportedVolumeFeaturesChanged;
		}

		/// <summary>
		/// Called when the wrapped volume control changes volume.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ActiveControlOnVolumeChanged(object sender, VolumeControlVolumeChangedApiEventArgs eventArgs)
		{
			UpdateState();
		}

		/// <summary>
		/// Called when the wrapped mute control changes mute state.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ActiveControlOnIsMutedChanged(object sender, VolumeControlIsMutedChangedApiEventArgs eventArgs)
		{
			UpdateState();
		}

		/// <summary>
		/// Called when the wrapped volume control changes supported features.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ActiveControlOnSupportedVolumeFeaturesChanged(object sender,
		                                                           VolumeControlSupportedVolumeFeaturesChangedApiEventArgs
			                                                           eventArgs)
		{
			UpdateState();
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(GenericAmpDevice parent)
		{
			base.Subscribe(parent);

			parent.Switcher.OnActiveInputsChanged += SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(GenericAmpDevice parent)
		{
			base.Subscribe(parent);

			parent.Switcher.OnActiveInputsChanged -= SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Called when the active input changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SwitcherOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs eventArgs)
		{
			IVolumeDeviceControl volumeControl = GetRoutedVolumeControl();
			SetActiveControl(volumeControl);
		}

		private IVolumeDeviceControl GetRoutedVolumeControl()
		{
			// This should never be null
			GenericAmpRouteSwitcherControl inputSelectControl = Parent.Controls.GetControl<GenericAmpRouteSwitcherControl>();
			if (inputSelectControl == null)
				return null;

			// If the active input is null then there is no active volume control
			int? activeInput = inputSelectControl.GetActiveInput(eConnectionType.Audio);
			if (!activeInput.HasValue)
				return null;
			
			// Is there a volume point associated with this input?
			IVolumePoint volumePoint = Parent.GetVolumePointForInput(activeInput.Value);
			if (volumePoint != null)
				return volumePoint.Control;

			// Walk the routing graph backwards to find the closest routed volume control
			IVolumeDeviceControl volumeControl = GetRoutedVolumeDeviceControl(inputSelectControl, activeInput.Value);
			return volumeControl == this ? null : volumeControl;
		}

		[CanBeNull]
		private IVolumeDeviceControl GetRoutedVolumeDeviceControl(IRouteDestinationControl destination, int input)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			IRoutingGraph graph;
			if (!Parent.Core.TryGetRoutingGraph(out graph))
				return null;

			while (true)
			{
				// Get the connection for the destination input
				Connection inputConnection = graph.Connections.GetInputConnection(destination, input);
				if (inputConnection == null || !inputConnection.ConnectionType.HasFlag(eConnectionType.Audio))
					return null;

				IRouteSourceControl sourceControl = graph.GetSourceControl(inputConnection);

				// Find the volume control on the source.
				IVolumeDeviceControl volumeControl = sourceControl.Parent.Controls.GetControl<IVolumeDeviceControl>();
				if (volumeControl != null)
					return volumeControl;

				// Did we reach a dead end?
				IRouteMidpointControl sourceAsMidpoint = sourceControl as IRouteMidpointControl;
				if (sourceAsMidpoint == null)
					return null;

				ConnectorInfo? sourceConnector = sourceAsMidpoint.GetInput(inputConnection.Source.Address, eConnectionType.Audio);
				if (!sourceConnector.HasValue)
					return null;

				destination = sourceAsMidpoint;
				input = sourceConnector.Value.Address;
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Active Volume Control", m_ActiveControl);
		}

		#endregion
	}
}
