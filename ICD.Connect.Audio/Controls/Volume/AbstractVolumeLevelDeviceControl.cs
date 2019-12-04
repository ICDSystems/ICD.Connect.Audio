using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Volume;
using ICD.Connect.Audio.Repeaters;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls.Volume
{
	public abstract class AbstractVolumeLevelDeviceControl<T> : AbstractVolumePercentDeviceControl<T>, IVolumeLevelDeviceControl
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

		private readonly VolumeLevelRepeater m_Repeater;

		private float? m_VolumeLevelMax;
		private float? m_VolumeLevelMin;

		public float IncrementValue
		{
			get { return m_Repeater.InitialIncrement; }
			set
			{
				m_Repeater.InitialIncrement = value;
				m_Repeater.RepeatIncrement = value;
			}
		}

		public override long RepeatBeforeTime
		{
			get { return m_Repeater.BeforeRepeat; }
			set
			{
				base.RepeatBeforeTime = value;
				m_Repeater.BeforeRepeat = value;
			}
		}

		public override long RepeatBetweenTime
		{
			get { return m_Repeater.BetweenRepeat; }
			set
			{
				base.RepeatBetweenTime = value;
				m_Repeater.BetweenRepeat = value;
			}
		}

		#region Properties

		/// <summary>
		/// Gets the current volume, in string representation
		/// </summary>
		public override string VolumeString { get { return ConvertLevelToString(VolumeLevel); } }

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public abstract float VolumeLevel { get; }

		/// <summary>
		/// Minimum value for the raw volume level
		/// This could be the minimum permitted by the device/control, or a safety min
		/// </summary>
		public virtual float? VolumeRawMin
		{
			get { return m_VolumeLevelMin; }
			set
			{
				m_VolumeLevelMin = value;
				if (m_VolumeLevelMin != null)
					ClampLevel();
			}
		}

		/// <summary>
		/// Maximum value for the raw volume level
		/// This could be the maximum permitted by the device/control, or a safety max
		/// </summary>
		public virtual float? VolumeRawMax
		{
			get { return m_VolumeLevelMax; }
			set
			{
				m_VolumeLevelMax = value;

				if (m_VolumeLevelMax != null)
					ClampLevel();
			}
		}

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeLevelMin
		{
			get { return VolumeRawMin == null ? VolumeRawMinAbsolute : Math.Max(VolumeRawMinAbsolute, (float)VolumeRawMin); }
		}

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeLevelMax
		{
			get { return VolumeRawMax == null ? VolumeRawMaxAbsolute : Math.Min(VolumeRawMaxAbsolute, (float)VolumeRawMax); }
		}

		/// <summary>
		/// Gets the percentage of the volume in the specified range
		/// </summary>
		public override float VolumePercent { get { return this.ConvertLevelToPercent(VolumeLevel); } }

		/// <summary>
		/// Gets the volume repeater for this instance.
		/// </summary>
		protected override IVolumeRepeater VolumeRepeater { get { return m_Repeater; } }

		/// <summary>
		/// Absolute Minimum the raw volume can be
		/// Used as a last resort for percent caculation
		/// </summary>
		protected abstract float VolumeRawMinAbsolute { get; }

		/// <summary>
		/// Absolute Maximum the raw volume can be
		/// Used as a last resport for percent caculation
		/// </summary>
		protected abstract float VolumeRawMaxAbsolute { get; }

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		protected AbstractVolumeLevelDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_Repeater = new VolumeLevelRepeater(DEFAULT_INCREMENT_VALUE,
												 DEFAULT_INCREMENT_VALUE,
			                                     DEFAULT_REPEAT_BEFORE_TIME,
			                                     DEFAULT_REPEAT_BETWEEN_TIME);
			m_Repeater.SetControl(this);
		}

		#region Methods

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public abstract void SetVolumeLevel(float volume);

		/// <summary>
		/// Sets the volume percent in the specified range
		/// </summary>
		/// <param name="percent"></param>
		public override void SetVolumePercent(float percent)
		{
			float level = this.ConvertPercentToLevel(percent);
			SetVolumeLevel(level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			this.VolumeLevelIncrement(IncrementValue);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			this.VolumeLevelDecrement(IncrementValue);
		}

		#endregion

		#region Private/Protected Methods

		private void ClampLevel()
		{
			float clampValue = this.ClampToVolumeLevel(VolumeLevel);
			if (Math.Abs(clampValue - VolumeLevel) > FLOAT_COMPARE_TOLERANCE)
				SetVolumeLevel(clampValue);
		}

		protected virtual void VolumeFeedback(float volumeLevel)
		{
			VolumeFeedback(volumeLevel, this.ConvertLevelToPercent(volumeLevel));
		}

		protected virtual string ConvertLevelToString(float level)
		{
			return level.ToString("n2");
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
