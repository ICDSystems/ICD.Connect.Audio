using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
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
		private const float FLOAT_COMPARE_TOLERANCE = 0.00001F;
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

		public virtual event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;
		
		#endregion

		#region Abstract Properties

		public abstract float VolumeRaw { get; }
		public abstract float VolumePosition { get; }

		#endregion

		#region Properties

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

		#region Abstract Methods

		public abstract void SetVolumeRaw(float volume);
		public abstract void SetVolumePosition(float position);

		public virtual void VolumeLevelIncrement(float incrementValue)
		{
			SetVolumeRaw(this.ClampRawVolume(VolumeRaw + incrementValue));
		}

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

		protected virtual void VolumeFeedback(VolumeDeviceVolumeChangedEventArgs args)
		{
			OnVolumeChanged.Raise(this, args);
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
	}
}
