using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls
{
	public abstract class AbstractVolumeLevelBasicDeviceControl<T> : AbstractDeviceControl<T>, IVolumeLevelBasicDeviceControl
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
		private readonly VolumeBasicRepeater m_Repeater;

		#region Properties

		/// <summary>
		/// Gets the volume repeater for this instance.
		/// </summary>
		protected virtual IVolumeRepeater VolumeRepeater { get { return m_Repeater; } }

		/// <summary>
		/// Time from the press to the repeat
		/// </summary>
		[PublicAPI]
		public long RepeatBeforeTime
		{
			get { return VolumeRepeater.BeforeRepeat; }
			set { VolumeRepeater.BeforeRepeat = value; }
		}

		/// <summary>
		/// Time between repeats
		/// </summary>
		[PublicAPI]
		public long RepeatBetweenTime
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
		protected AbstractVolumeLevelBasicDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_Repeater = new VolumeBasicRepeater(DEFAULT_REPEAT_BEFORE_TIME, DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Methods

		/// <summary>
		/// Volume Level Increment
		/// </summary>
		public abstract void VolumeLevelIncrement();

		/// <summary>
		/// Volume Level Decrement
		/// </summary>
		public abstract void VolumeLevelDecrement();

		/// <summary>
		/// Starts a volume ramp up operation
		/// VolumeLevelRampStop() must be called to stop the ramping
		/// </summary>
		public void VolumeLevelRampUp()
		{
			VolumeLevelRamp(true);
		}

		/// <summary>
		/// Starts a volume ramp down operation
		/// VolumeLevelRampStop() must be called to stop the ramping
		/// </summary>
		public void VolumeLevelRampDown()
		{
			VolumeLevelRamp(false);
		}

		/// <summary>
		/// Stops the volume ramp and disposes of the repeater timer
		/// </summary>
		public void VolumeLevelRampStop()
		{
			VolumeRepeater.Release();
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

			foreach (IConsoleNodeBase node in VolumeLevelBasicDeviceControlConsole.GetConsoleNodes(this))
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

			VolumeLevelBasicDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeLevelBasicDeviceControlConsole.GetConsoleCommands(this))
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
