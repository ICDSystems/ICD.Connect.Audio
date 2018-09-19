﻿using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Extensions;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Devices
{
	public sealed class GenericAmpVolumeControl : AbstractDeviceControl<GenericAmpDevice>, IVolumeLevelDeviceControl,
	                                              IVolumeMuteFeedbackDeviceControl
	{
		#region Events

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		private GenericAmpRouteSwitcherControl m_Switcher;

		private int? m_ActiveDeviceId;
		private int? m_ActiveControlId;

		#region Properties

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public float VolumeRaw { get { return ActiveControlAction<IVolumeLevelDeviceControl, float>(c => c.VolumeRaw); } }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public float VolumePosition { get { return ActiveControlAction<IVolumePositionDeviceControl, float>(c => c.VolumePosition); } }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public string VolumeString { get { return ActiveControlAction<IVolumePositionDeviceControl, string>(c => c.VolumeString); } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeRawMaxRange { get { return ActiveControlAction<IVolumeLevelDeviceControl, float>(c => c.VolumeRawMaxRange); } }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeRawMinRange { get { return ActiveControlAction<IVolumeLevelDeviceControl, float>(c => c.VolumeRawMinRange); } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted { get { return ActiveControlAction<IVolumeMuteFeedbackDeviceControl, bool>(c => c.VolumeIsMuted); } }

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

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnVolumeChanged = null;
			OnMuteStateChanged = null;

			Unsubscribe(Parent);

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeIncrement()
		{
			ActiveControlAction<IVolumePositionDeviceControl>(c => c.VolumeIncrement());
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeDecrement()
		{
			ActiveControlAction<IVolumePositionDeviceControl>(c => c.VolumeDecrement());
		}

		/// <summary>
		/// Starts raising the volume, and continues until RampStop is called.
		/// <see cref="VolumeRampStop"/> must be called after
		/// </summary>
		public void VolumeRampUp()
		{
			ActiveControlAction<IVolumeRampDeviceControl>(c => c.VolumeRampUp());
		}

		/// <summary>
		/// Starts lowering the volume, and continues until RampStop is called.
		/// <see cref="VolumeRampStop"/> must be called after
		/// </summary>
		public void VolumeRampDown()
		{
			ActiveControlAction<IVolumeRampDeviceControl>(c => c.VolumeRampDown());
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeRampStop()
		{
			ActiveControlAction<IVolumeRampDeviceControl>(c => c.VolumeRampStop());
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public void SetVolumeRaw(float volume)
		{
			ActiveControlAction<IVolumeLevelDeviceControl>(c => c.SetVolumeRaw(volume));
		}

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public void SetVolumePosition(float position)
		{
			ActiveControlAction<IVolumePositionDeviceControl>(c => c.SetVolumePosition(position));
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeLevelIncrement(float incrementValue)
		{
			ActiveControlAction<IVolumeLevelDeviceControl>(c => c.VolumeLevelIncrement(incrementValue));
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeLevelDecrement(float decrementValue)
		{
			ActiveControlAction<IVolumeLevelDeviceControl>(c => c.VolumeLevelIncrement(decrementValue));
		}

		/// <summary>
		/// Starts raising the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumePositionRampUp(float increment)
		{
			ActiveControlAction<IVolumePositionDeviceControl>(c => c.VolumePositionRampUp(increment));
		}

		/// <summary>
		/// Starts lowering the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumePositionRampDown(float decrement)
		{
			ActiveControlAction<IVolumePositionDeviceControl>(c => c.VolumePositionRampDown(decrement));
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			ActiveControlAction<IVolumeMuteBasicDeviceControl>(c => c.VolumeMuteToggle());
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			ActiveControlAction<IVolumeMuteDeviceControl>(c => c.SetVolumeMute(mute));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Helper method for performing an action for the given control type on the current active device.
		/// Returns default result if there is no active device or the control is null.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <param name="callback"></param>
		/// <returns></returns>
		private void ActiveControlAction<TControl>(Action<TControl> callback)
			where TControl : class, IVolumeDeviceControl
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			TControl control = GetActiveControl<TControl>();

			if (control != null)
				callback(control);
		}

		/// <summary>
		/// Helper method for performing an action for the given control type on the current active device.
		/// Returns default result if there is no active device or the control is null.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="callback"></param>
		/// <returns></returns>
		private TResult ActiveControlAction<TControl, TResult>(Func<TControl, TResult> callback)
			where TControl : class, IVolumeDeviceControl
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			TResult output = default(TResult);
			ActiveControlAction<TControl>(c => output = callback(c));
			return output;
		}

		/// <summary>
		/// Gets the current active volume control of the given type.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <returns></returns>
		[CanBeNull]
		private TControl GetActiveControl<TControl>()
			where TControl : class, IVolumeDeviceControl
		{
			if (!m_ActiveDeviceId.HasValue)
				return default(TControl);

			IDeviceBase device =
				ServiceProvider.GetService<ICore>()
				               .Originators
				               .GetChild<IDeviceBase>(m_ActiveDeviceId.Value);

			int controlId = m_ActiveControlId ?? 0;
			return device.Controls.GetControl<TControl>(controlId);
		}

		private void SetActiveControl(int? device, int? control)
		{
			if (device == m_ActiveDeviceId && control == m_ActiveControlId)
				return;

			// Mute the old active device
			SetVolumeMute(true);

			m_ActiveDeviceId = device;
			m_ActiveControlId = control;

			// Unmute the next active device
			// TODO - Track the selected mute state to maintain between switching
			SetVolumeMute(false);
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Subscribe(GenericAmpDevice parent)
		{
			m_Switcher = parent.Controls.GetControl<GenericAmpRouteSwitcherControl>();
			if (m_Switcher == null)
				throw new InvalidOperationException("Could not find switcher control on parent device.");

			m_Switcher.OnActiveInputsChanged += SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Unsubscribe(GenericAmpDevice parent)
		{
			if (m_Switcher == null)
				return;

			m_Switcher.OnActiveInputsChanged -= SwitcherOnActiveInputsChanged;
		}

		/// <summary>
		/// Called when the active input changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SwitcherOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs eventArgs)
		{
			int? deviceId;
			int? controlId;

			GetRoutedDeviceAndControl(out deviceId, out controlId);

			if (deviceId == Parent.Id)
			{
				deviceId = null;
				controlId = null;

				Logger.AddEntry(eSeverity.Warning, "{0} - Attempted to control own volume recursively", this);
			}

			SetActiveControl(deviceId, controlId);
		}

		private void GetRoutedDeviceAndControl(out int? device, out int? control)
		{
			device = null;
			control = null;

			// This should never be null
			GenericAmpRouteSwitcherControl inputSelectControl = Parent.Controls.GetControl<GenericAmpRouteSwitcherControl>();
			if (inputSelectControl == null)
				return;

			// If the active input is null then there is no active volume control
			int? activeInput = inputSelectControl.ActiveInput;
			if (!activeInput.HasValue)
				return;
			
			// Is there a volume point associated with this input?
			IVolumePoint volumePoint = Parent.GetVolumePointForInput(activeInput.Value);
			if (volumePoint != null)
			{
				device = volumePoint.DeviceId;
				control = volumePoint.ControlId;
				return;
			}

			// Walk the routing graph backwards to find the closest routed volume control
			IVolumeDeviceControl volumeControl = GetRoutedVolumeDeviceControl(inputSelectControl, activeInput.Value);
			device = volumeControl == null ? (int?)null : volumeControl.Parent.Id;
			control = volumeControl == null ? (int?)null : volumeControl.Id;
		}

		[CanBeNull]
		private IVolumeDeviceControl GetRoutedVolumeDeviceControl(IRouteDestinationControl destination, int input)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			IRoutingGraph graph;
			if (!ServiceProvider.GetService<ICore>().TryGetRoutingGraph(out graph))
				return null;

			while (true)
			{
				// Get the connection for the destination input
				Connection inputConnection = graph.Connections.GetInputConnection(destination, input);
				if (inputConnection == null || !inputConnection.ConnectionType.HasFlag(eConnectionType.Audio))
					return null;

				IRouteSourceControl sourceControl = graph.GetSourceControl(inputConnection);
				if (sourceControl == null)
					return null;

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

			addRow("Active Volume Control", GetActiveControl<IVolumeDeviceControl>());
		}

		#endregion
	}
}
