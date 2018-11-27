using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls
{
	public abstract class AbstractVolumePositionDeviceControl<T> : AbstractVolumeRampDeviceControl<T>, IVolumePositionDeviceControl
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

		private readonly VolumePositionRepeater m_Repeater;

		#region Properties

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public abstract float VolumePosition { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public virtual string VolumeString { get { return ConvertPositionToString(VolumePosition); } }

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
		protected AbstractVolumePositionDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_Repeater = new VolumePositionRepeater(DEFAULT_INCREMENT_VALUE,
			                                        DEFAULT_INCREMENT_VALUE,
			                                        DEFAULT_REPEAT_BEFORE_TIME,
			                                        DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Methods

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public abstract void SetVolumePosition(float position);

		/// <summary>
		/// Starts raising the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumePositionRampUp(float increment)
		{
			m_Repeater.VolumeUpHoldPosition(increment);
		}

		/// <summary>
		/// Starts lowering the volume in steps of the given position, and continues until RampStop is called.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumePositionRampDown(float decrement)
		{
			m_Repeater.VolumeDownHoldPosition(decrement);
		}

		/// <summary>
		/// Volume Level Increment
		/// </summary>
		public override void VolumeIncrement()
		{
			VolumePositionIncrement(DEFAULT_INCREMENT_VALUE);
		}

		/// <summary>
		/// Volume Level Decrement
		/// </summary>
		public override void VolumeDecrement()
		{
			VolumePositionDecrement(DEFAULT_INCREMENT_VALUE);
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public virtual void VolumePositionIncrement(float incrementValue)
		{
			float newPosition = MathUtils.Clamp(VolumePosition + incrementValue, 0.0f, 1.0f);
			SetVolumePosition(newPosition);
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public virtual void VolumePositionDecrement(float decrementValue)
		{
			float newPosition = MathUtils.Clamp(VolumePosition - decrementValue, 0.0f, 1.0f);
			SetVolumePosition(newPosition);
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

		protected virtual string ConvertPositionToString(float position)
		{
			return string.Format("{0}%", (int)position);
		}

		protected virtual void VolumeFeedback(float volumeRaw, float volumePosition)
		{
			VolumeFeedback(volumeRaw, volumePosition, VolumeString);
		}

		protected virtual void VolumeFeedback(float volumeRaw, float volumePosition, string volumeString)
		{
			OnVolumeChanged.Raise(this, new VolumeDeviceVolumeChangedEventArgs(volumeRaw, volumePosition, volumeString));
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

			foreach (IConsoleNodeBase node in VolumePositionDeviceControlConsole.GetConsoleNodes(this))
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

			VolumePositionDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumePositionDeviceControlConsole.GetConsoleCommands(this))
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
