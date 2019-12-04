using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls.Volume
{
	public abstract class AbstractVolumePercentDeviceControl<T> : AbstractVolumeRampDeviceControl<T>, IVolumePercentDeviceControl
		where T : IDeviceBase
	{
		/// <summary>
		/// Default value to increment by
		/// </summary>
		private const float DEFAULT_INCREMENT_VALUE = 1.0f / 100.0f;

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public virtual event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		private readonly VolumePercentRepeater m_Repeater;

		#region Properties

		/// <summary>
		/// Gets the current volume percent, 0 - 1
		/// </summary>
		public abstract float VolumePercent { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public virtual string VolumeString { get { return string.Format("{0:n2}%", VolumePercent * 100.0f); } }

		/// <summary>
		/// Gets the volume repeater for this instance.
		/// </summary>
		protected override IVolumeRepeater VolumeRepeater { get { return m_Repeater; } }

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		protected AbstractVolumePercentDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_Repeater = new VolumePercentRepeater(DEFAULT_INCREMENT_VALUE,
			                                       DEFAULT_INCREMENT_VALUE,
			                                       DEFAULT_REPEAT_BEFORE_TIME,
			                                       DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Methods

		/// <summary>
		/// Sets the volume percent, from 0-1
		/// </summary>
		/// <param name="percent"></param>
		public abstract void SetVolumePercent(float percent);

		/// <summary>
		/// Starts raising the volume in steps of the given percent, and continues until RampStop is called.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumePercentRampUp(float increment)
		{
			m_Repeater.VolumeUpHoldPercent(increment);
		}

		/// <summary>
		/// Starts lowering the volume in steps of the given percent, and continues until RampStop is called.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumePercentRampDown(float decrement)
		{
			m_Repeater.VolumeDownHoldPercent(decrement);
		}

		/// <summary>
		/// Volume Level Increment
		/// </summary>
		public override void VolumeIncrement()
		{
			VolumePercentIncrement(DEFAULT_INCREMENT_VALUE);
		}

		/// <summary>
		/// Volume Level Decrement
		/// </summary>
		public override void VolumeDecrement()
		{
			VolumePercentDecrement(DEFAULT_INCREMENT_VALUE);
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		private void VolumePercentIncrement(float incrementValue)
		{
			SetVolumePercent(VolumePercent + incrementValue);
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		private void VolumePercentDecrement(float decrementValue)
		{
			SetVolumePercent(VolumePercent - decrementValue);
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			base.VolumeRampStop();

			m_Repeater.Release();
		}

		#endregion

		#region Private/Protected Methods

		protected void VolumeFeedback(float level, float percent)
		{
			VolumeFeedback(level, percent, VolumeString);
		}

		protected void VolumeFeedback(float level, float percent, string volumeString)
		{
			Log(eSeverity.Informational, "Volume changed: Level={0} Percent={1} Name={2}", level, percent, volumeString);

			OnVolumeChanged.Raise(this, new VolumeDeviceVolumeChangedEventArgs(level, percent, volumeString));
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

			foreach (IConsoleNodeBase node in VolumePercentDeviceControlConsole.GetConsoleNodes(this))
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

			VolumePercentDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumePercentDeviceControlConsole.GetConsoleCommands(this))
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
