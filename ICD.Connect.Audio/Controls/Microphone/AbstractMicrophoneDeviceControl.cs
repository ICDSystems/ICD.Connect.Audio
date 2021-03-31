using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls.Microphone
{
	public abstract class AbstractMicrophoneDeviceControl<T> : AbstractVolumeDeviceControl<T>, IMicrophoneDeviceControl
		where T : IDevice
	{
		/// <summary>
		/// Raised when the phantom power state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPhantomPowerStateChanged;

		/// <summary>
		/// Raised when the gain level changes.
		/// </summary>
		public event EventHandler<FloatEventArgs> OnAnalogGainLevelChanged;

		private bool m_PhantomPower;
		private float m_AnalogGainLevel;

		#region Properties

		/// <summary>
		/// Gets the phantom power state.
		/// </summary>
		public bool PhantomPower
		{
			get { return m_PhantomPower; }
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
		public float AnalogGainLevel
		{
			get { return m_AnalogGainLevel; }
			protected set
			{
				const double tolerance = 0.001f;
				if (Math.Abs(value - m_AnalogGainLevel) < tolerance)
					return;

				m_AnalogGainLevel = value;

				Logger.LogSetTo(eSeverity.Informational, "GainLevel", m_AnalogGainLevel);

				OnAnalogGainLevelChanged.Raise(this, new FloatEventArgs(m_AnalogGainLevel));
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
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		protected AbstractMicrophoneDeviceControl(T parent, int id, Guid uuid)
			: base(parent, id, uuid)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnPhantomPowerStateChanged = null;
			OnAnalogGainLevelChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="level"></param>
		public abstract void SetAnalogGainLevel(float level);

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		public abstract void SetPhantomPower(bool power);

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
