using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.ClockAudio.Devices.CCRM4000
{
	public sealed class ClockAudioCcrm4000Device : AbstractDevice<ClockAudioCcrm4000DeviceSettings>
	{
		/// <summary>
		/// Timer to reset all relays to open state
		/// </summary>
		private readonly SafeTimer m_ResetTimer;

		private IRelayPort m_ExtendRelay;
		private IRelayPort m_RetractRelay;

		#region Properties

		/// <summary>
		/// Gets/sets the relay latch mode.
		/// </summary>
		public bool RelayLatch { get; set; }

		/// <summary>
		/// Gets/sets the relay hold time (in milliseconds).
		/// </summary>
		public long RelayHoldTime { get; set; }

		/// <summary>
		/// Gets the extend relay.
		/// </summary>
		[CanBeNull]
		public IRelayPort ExtendRelay { get { return m_ExtendRelay; } }

		/// <summary>
		/// Gets the retract relay.
		/// </summary>
		[CanBeNull]
		public IRelayPort RetractRelay { get { return m_RetractRelay; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ClockAudioCcrm4000Device()
		{
			m_ResetTimer = SafeTimer.Stopped(OpenAllRelays);
		}

		#region Methods

		/// <summary>
		/// Sets the extend relay.
		/// </summary>
		/// <param name="relay"></param>
		public void SetExtendRelay(IRelayPort relay)
		{
			if (relay == m_ExtendRelay)
				return;

			Unsubscribe(m_ExtendRelay);
			m_ExtendRelay = relay;
			Subscribe(m_ExtendRelay);
         
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Sets the retract relay.
		/// </summary>
		/// <param name="relay"></param>
		public void SetRetractRelay(IRelayPort relay)
		{
			if (relay == m_RetractRelay)
				return;

			Unsubscribe(m_RetractRelay);
			m_RetractRelay = relay;
			Subscribe(m_RetractRelay);

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Opens the retract relay and closes the extend relay.
		/// </summary>
		public void Extend()
		{
			ToggleRelays(m_ExtendRelay, m_RetractRelay);
		}

		/// <summary>
		/// Opens the extend relay and closes the retract relay.
		/// </summary>
		public void Retract()
		{
			ToggleRelays(m_RetractRelay, m_ExtendRelay);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ExtendRelay != null &&
			       m_ExtendRelay.IsOnline &&
			       m_RetractRelay != null &&
			       m_RetractRelay.IsOnline;
		}

		/// <summary>
		/// This is called when the reset timer expires - this opens all relays.
		/// </summary>
		private void OpenAllRelays()
		{
			if (m_ExtendRelay != null)
				m_ExtendRelay.Open();

			if (m_RetractRelay != null)
				m_RetractRelay.Open();
		}

		private void ToggleRelays(IRelayPort active, IRelayPort compliment)
		{
			// Cancel reset timer, if in progress
			m_ResetTimer.Stop();

			// Open Compliment Relay
			if (compliment != null)
				compliment.Open();

			// Close Active Relay
			if (active != null)
				active.Close();

			// If not in latch mode, set a timer to open relays
			if (!RelayLatch)
				m_ResetTimer.Reset(RelayHoldTime);
		}

		#endregion

		#region Relay Callbacks

		/// <summary>
		/// Subscribe to the relay events.
		/// </summary>
		/// <param name="relay"></param>
		private void Subscribe(IRelayPort relay)
		{
			if (relay == null)
				return;

			relay.OnIsOnlineStateChanged += RelayOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the relay events.
		/// </summary>
		/// <param name="relay"></param>
		private void Unsubscribe(IRelayPort relay)
		{
			if (relay == null)
				return;

			relay.OnIsOnlineStateChanged -= RelayOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when a relay's online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void RelayOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs eventArgs)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(ClockAudioCcrm4000DeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			// Extend relay
			IRelayPort extendRelay = null;
			if (settings.ExtendRelay != null)
			{
				try
				{
					extendRelay = factory.GetOriginatorById<IRelayPort>(settings.ExtendRelay.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No relay with id {0}", settings.ExtendRelay);
				}	
			}
			SetExtendRelay(extendRelay);

			// Retract relay
			IRelayPort retractRelay = null;
			if (settings.RetractRelay != null)
			{
				try
				{
					retractRelay = factory.GetOriginatorById<IRelayPort>(settings.RetractRelay.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No relay with id {0}", settings.RetractRelay);
				}
			}
			SetRetractRelay(retractRelay);

			// Additional Parameters
			RelayLatch = settings.RelayLatch;
			RelayHoldTime = settings.RelayHoldTime;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(ClockAudioCcrm4000DeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.ExtendRelay = m_ExtendRelay == null ? null : (int?)m_ExtendRelay.Id;
			settings.RetractRelay = m_RetractRelay == null ? null : (int?)m_RetractRelay.Id;
			settings.RelayLatch = RelayLatch;
			settings.RelayHoldTime = RelayHoldTime;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetExtendRelay(null);
			SetRetractRelay(null);
			RelayLatch = false;
			RelayHoldTime = 500;
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(ClockAudioCcrm4000DeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new ClockAudioCcrm4000RoutSourceControl(this, 0));
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

			addRow("Latch Relay", RelayLatch);
			addRow("Relay Hold Time", RelayHoldTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Extend", "Extends the microphone", () => Extend());
			yield return new ConsoleCommand("Retract", "Retracts the microphone", () => Retract());
			yield return new ConsoleCommand("OpenRelays", "Opens all relays", () => OpenAllRelays());
			yield return new GenericConsoleCommand<long>("SetRelayHoldTime", "How long to hold relays closed, in ms", i => RelayHoldTime = i);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
