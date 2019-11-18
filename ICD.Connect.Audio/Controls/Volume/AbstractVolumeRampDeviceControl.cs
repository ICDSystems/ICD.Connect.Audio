using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Volume;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls.Volume
{
	public abstract class AbstractVolumeRampDeviceControl<T> : AbstractVolumeDeviceControl<T>, IVolumeRampDeviceControl
		where T : IDeviceBase
	{
		#region Constants

		/// <summary>
		/// Default time before repeat for volume ramping operations
		/// </summary>
		protected const long DEFAULT_REPEAT_BEFORE_TIME = 250;

		/// <summary>
		/// Default time between repeats for volume ramping operations
		/// </summary>
		protected const long DEFAULT_REPEAT_BETWEEN_TIME = 250;

		#endregion

		/// <summary>
		/// Repeater for volume ramping operaions
		/// </summary>
		private readonly VolumeRampRepeater m_Repeater;

		#region Properties

		/// <summary>
		/// Gets the volume repeater for this instance.
		/// </summary>
		protected virtual IVolumeRepeater VolumeRepeater { get { return m_Repeater; } }

		/// <summary>
		/// Time from the press to the repeat
		/// </summary>
		public virtual long RepeatBeforeTime
		{
			get { return VolumeRepeater.BeforeRepeat; }
			set { VolumeRepeater.BeforeRepeat = value; }
		}

		/// <summary>
		/// Time between repeats
		/// </summary>
		public virtual long RepeatBetweenTime
		{
			get { return VolumeRepeater.BetweenRepeat; }
			set { VolumeRepeater.BetweenRepeat = value; }
		}

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		protected AbstractVolumeRampDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_Repeater = new VolumeRampRepeater(DEFAULT_REPEAT_BEFORE_TIME, DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Methods

		/// <summary>
		/// Default volume increment.
		/// </summary>
		public abstract void VolumeIncrement();

		/// <summary>
		/// Default volume increment.
		/// </summary>
		public abstract void VolumeDecrement();

		/// <summary>
		/// Starts a volume ramp up operation
		/// VolumeLevelRampStop() must be called to stop the ramping
		/// </summary>
		public void VolumeRampUp()
		{
			VolumeLevelRamp(true);
		}

		/// <summary>
		/// Starts a volume ramp down operation
		/// VolumeLevelRampStop() must be called to stop the ramping
		/// </summary>
		public void VolumeRampDown()
		{
			VolumeLevelRamp(false);
		}

		/// <summary>
		/// Stops the volume ramp and disposes of the repeater timer
		/// </summary>
		public virtual void VolumeRampStop()
		{
			m_Repeater.Release();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates the repeater timer and starts up/down ramp
		/// </summary>
		/// <param name="up">true for up ramp, false for down ramp</param>
		private void VolumeLevelRamp(bool up)
		{
			VolumeRepeater.VolumeHold(up);
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

			foreach (IConsoleNodeBase node in VolumeRampDeviceControlConsole.GetConsoleNodes(this))
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

			VolumeRampDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeRampDeviceControlConsole.GetConsoleCommands(this))
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
