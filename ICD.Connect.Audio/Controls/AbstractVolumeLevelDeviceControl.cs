﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls
{
	public abstract class AbstractVolumeLevelDeviceControl<T> : AbstractDeviceControl<T>, IVolumeLevelDeviceControl
		where T : IDeviceBase
	{
		#region Constants

		/// <summary>
		/// Default time before repeat for volume ramping operations
		/// </summary>
		private const int DEFAULT_REPEAT_BEFORE_TIME = 250;

		/// <summary>
		/// Default time between repeats for volume ramping operations
		/// </summary>
		private const int DEFAULT_REPEAT_BETWEEN_TIME = 250;

		/// <summary>
		/// Default value to increment by
		/// </summary>
		private const float DEFAULT_INCREMENT_VALUE = 1;

		/// <summary>
		/// Tolerance for float comparisons
		/// </summary>
		private const float FLOAT_COMPARE_TOLERANCE = 0.00001f;

		#endregion

		#region Fields

		private float? m_IncrementValue;

		/// <summary>
		/// Repeater for volume ramping operaions
		/// </summary>
		private VolumeRepeater m_Repeater;

		/// <summary>
		/// Used when creating/accessing/disposing repeater
		/// </summary>
		private readonly SafeCriticalSection m_RepeaterCriticalSection;

		private int? m_RepeatBeforeTime;
		private int? m_RepeatBetweenTime;

		private float? m_VolumeRawMax;
		private float? m_VolumeRawMin;

		#endregion

		#region Events

		/// <summary>
		/// Raised when the raw volume changes.
		/// </summary>
		public virtual event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;
		
		#endregion

		#region Abstract Properties

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public abstract float VolumeRaw { get; }

		/// <summary>
		/// Gets the current volume positon, 0 - 1
		/// </summary>
		public abstract float VolumePosition { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public virtual string VolumeString
		{
			get { return ConvertLevelToString(VolumeRaw); }
		}

		public float IncrementValue
		{
			get
			{
				if (m_IncrementValue != null)
					return (float)m_IncrementValue;
				return DEFAULT_INCREMENT_VALUE;
			}
			set
			{
				if (Math.Abs(value) > FLOAT_COMPARE_TOLERANCE)
					m_IncrementValue = value;
				else
					m_IncrementValue = null;
			}
		}

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public virtual float? VolumeRawMax
		{
			get
			{
				return m_VolumeRawMax;
			}
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
			get
			{
				return m_VolumeRawMin;
			}
			set
			{
				m_VolumeRawMin = value;
				if (m_VolumeRawMin != null)
					ClampLevel();
			}
		}

		/// <summary>
		/// Time from the press to the repeat
		/// </summary>
		[PublicAPI]
		public virtual int RepeatBeforeTime
		{
			get
			{
				if (m_RepeatBeforeTime != null)
					return (int)m_RepeatBeforeTime;
				return DEFAULT_REPEAT_BEFORE_TIME;
			}
			set
			{
				if (Math.Abs(value) > FLOAT_COMPARE_TOLERANCE)
					m_RepeatBeforeTime = value;
				else
					m_RepeatBeforeTime = null;
			}
		}

		/// <summary>
		/// Time between repeats
		/// </summary>
		[PublicAPI]
		public virtual int RepeatBetweenTime
		{
			get
			{
				if (m_RepeatBetweenTime != null)
					return (int)m_RepeatBetweenTime;
				return DEFAULT_REPEAT_BETWEEN_TIME;
			}
			set
			{
				if (Math.Abs(value) > FLOAT_COMPARE_TOLERANCE)
					m_RepeatBetweenTime = value;
				else
					m_RepeatBetweenTime = null;
			}
		}

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		protected AbstractVolumeLevelDeviceControl(T parent, int id) : base(parent, id)
		{
			m_RepeaterCriticalSection = new SafeCriticalSection();
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
			SetVolumeRaw(this.ClampRawVolume(VolumeRaw + incrementValue));
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public virtual void VolumeLevelDecrement(float decrementValue)
		{
			SetVolumeRaw(this.ClampRawVolume(VolumeRaw - decrementValue));
		}

		#endregion

		#region Methods

		/// <summary>
		/// Volume Level Increment
		/// </summary>
		public virtual void VolumeLevelIncrement()
		{
			VolumeLevelIncrement(IncrementValue);
		}

		/// <summary>
		/// Volume Level Decrement
		/// </summary>
		public virtual void VolumeLevelDecrement()
		{
			VolumeLevelDecrement(IncrementValue);
		}

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
			m_RepeaterCriticalSection.Enter();
			try
			{
				if (m_Repeater == null)
					return;

				m_Repeater.Release();
				m_Repeater.Dispose();
				m_Repeater = null;
			}
			finally
			{
				m_RepeaterCriticalSection.Leave();
			}
		}

		public virtual string ConvertLevelToString(float level)
		{
			return level.ToString("n2");
		}

		#endregion

		#region Private/Protected Methods

		/// <summary>
		/// Creates the repeater timer and starts up/down ramp
		/// </summary>
		/// <param name="up">true for up ramp, false for down ramp</param>
		private void VolumeLevelRamp(bool up)
		{
			m_RepeaterCriticalSection.Enter();
			try
			{
				if (m_Repeater == null)
				{
					// Use m_IncrementValue to capture null
					// VolumeRepater will call VolumeLevelIncrement() when incrementvalue is null
					m_Repeater = new VolumeRepeater(m_IncrementValue, m_IncrementValue, RepeatBeforeTime, RepeatBetweenTime);
					m_Repeater.SetControl(this);
				}
				else
					m_Repeater.Release();

				m_Repeater.VolumeHold(up);
			}
			finally
			{
				m_RepeaterCriticalSection.Leave();
			}
		}

		protected void ClampLevel()
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

			foreach (IConsoleNodeBase node in VolumeLevelDeviceControlConsole.GetConsoleNodes(this))
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

			VolumeLevelDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeLevelDeviceControlConsole.GetConsoleCommands(this))
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
