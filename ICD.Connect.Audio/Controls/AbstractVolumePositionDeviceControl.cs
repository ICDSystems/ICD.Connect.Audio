using System;
using System.Collections.Generic;
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
		private const float DEFAULT_INCREMENT_VALUE = 1;

		/// <summary>
		/// Tolerance for float comparisons
		/// </summary>
		private const float FLOAT_COMPARE_TOLERANCE = 0.00001f;

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public virtual event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;

		private readonly VolumeLevelRepeater m_Repeater;

		private float? m_VolumeRawMax;
		private float? m_VolumeRawMin;

		#region Properties

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public abstract float VolumeRaw { get; }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public abstract float VolumePosition { get; }

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public virtual string VolumeString { get { return ConvertLevelToString(VolumeRaw); } }

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public virtual float? VolumeRawMax
		{
			get { return m_VolumeRawMax; }
			set
			{
				m_VolumeRawMax = value;

				if (m_VolumeRawMax != null)
					ClampLevel();
			}
		}

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		public virtual float? VolumeRawMin
		{
			get { return m_VolumeRawMin; }
			set
			{
				m_VolumeRawMin = value;
				if (m_VolumeRawMin != null)
					ClampLevel();
			}
		}

		public float InitialIncrement
		{
			get { return m_Repeater.InitialIncrement; }
			set { m_Repeater.InitialIncrement = value; }
		}

		public float RepeatIncrement
		{
			get { return m_Repeater.RepeatIncrement; }
			set { m_Repeater.RepeatIncrement = value; }
		}

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
			m_Repeater = new VolumeLevelRepeater(DEFAULT_INCREMENT_VALUE,
			                                DEFAULT_INCREMENT_VALUE,
			                                DEFAULT_REPEAT_BEFORE_TIME,
			                                DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Abstract Methods

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public abstract void SetVolumeRaw(float volume);

		/// <summary>
		/// Sets the volume position, from 0-1
		/// </summary>
		/// <param name="position"></param>
		public abstract void SetVolumePosition(float position);

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public virtual void VolumeLevelIncrement(float incrementValue)
		{
			float newRaw = this.ClampRawVolume(VolumeRaw + incrementValue);
			SetVolumeRaw(newRaw);
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public virtual void VolumeLevelDecrement(float decrementValue)
		{
			float newRaw = this.ClampRawVolume(VolumeRaw - decrementValue);
			SetVolumeRaw(newRaw);
		}

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

		#endregion

		#region Methods

		/// <summary>
		/// Volume Level Increment
		/// </summary>
		public override void VolumeIncrement()
		{
			VolumeLevelIncrement(InitialIncrement);
		}

		/// <summary>
		/// Volume Level Decrement
		/// </summary>
		public override void VolumeDecrement()
		{
			VolumeLevelDecrement(InitialIncrement);
		}

		protected virtual string ConvertLevelToString(float level)
		{
			return level.ToString("n2");
		}

		#endregion

		#region Private/Protected Methods

		private void ClampLevel()
		{
			float clampValue = this.ClampRawVolume(VolumeRaw);
			if (Math.Abs(clampValue - VolumeRaw) > FLOAT_COMPARE_TOLERANCE)
				SetVolumeRaw(clampValue);
		}

		protected virtual void VolumeFeedback(float volumeRaw, float volumePosition)
		{
			VolumeFeedback(volumeRaw, volumePosition, ConvertLevelToString(volumeRaw));
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
