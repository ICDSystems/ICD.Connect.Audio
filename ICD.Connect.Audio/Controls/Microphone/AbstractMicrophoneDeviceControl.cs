using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Microphone;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using Activity = ICD.Common.Logging.LoggingContexts.Activity;

namespace ICD.Connect.Audio.Controls.Microphone
{
	public abstract class AbstractMicrophoneDeviceControl<T> : AbstractDeviceControl<T>, IMicrophoneDeviceControl
		where T : IDevice
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the phantom power state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPhantomPowerStateChanged;

		/// <summary>
		/// Raised when the gain level changes.
		/// </summary>
		public event EventHandler<FloatEventArgs> OnGainLevelChanged;

		private bool m_IsMuted;
		private bool m_PhantomPower;
		private float m_GainLevel;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			protected set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Logger.LogSetTo(eSeverity.Informational, "IsMuted", m_IsMuted);
				Activities.LogActivity(m_IsMuted
									   ? new Activity(Activity.ePriority.Medium, "IsMuted", "Muted", eSeverity.Informational)
									   : new Activity(Activity.ePriority.Low, "IsMuted", "Unmuted", eSeverity.Informational));

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// Gets the phantom power state.
		/// </summary>
		public bool PhantomPower
		{
			get { return m_PhantomPower;}
			protected set
			{
				if (value == m_PhantomPower)
					return;

				m_PhantomPower = value;

				Logger.LogSetTo(eSeverity.Informational, "PhantomPower", m_PhantomPower);

				OnPhantomPowerStateChanged.Raise(this, new BoolEventArgs(m_PhantomPower));
			}
		}

		/// <summary>
		/// Gets the gain level.
		/// </summary>
		public float GainLevel
		{
			get { return m_GainLevel; }
			protected set
			{
				const double tolerance = 0.001f;
				if (Math.Abs(value - m_GainLevel) < tolerance)
					return;

				m_GainLevel = value;

				Logger.LogSetTo(eSeverity.Informational, "GainLevel", m_GainLevel);

				OnGainLevelChanged.Raise(this, new FloatEventArgs(m_GainLevel));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractMicrophoneDeviceControl(T parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnMuteStateChanged = null;
			OnPhantomPowerStateChanged = null;
			OnGainLevelChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		public abstract void SetGainLevel(float volume);

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		public abstract void SetMuted(bool mute);

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		public abstract void SetPhantomPower(bool power);

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public virtual void MuteToggle()
		{
			SetMuted(!IsMuted);
		}

		/// <summary>
		/// Toggles the current phantom power state.
		/// </summary>
		public virtual void PhantomPowerToggle()
		{
			SetPhantomPower(!PhantomPower);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in MicrophoneDeviceControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Wrokaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			MicrophoneDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in MicrophoneDeviceControlConsole.GetConsoleCommands(this))
				yield return command;
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
