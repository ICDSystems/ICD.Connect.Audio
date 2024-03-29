﻿using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.VolumePoints;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public sealed class VolumeRepeater : IDisposable
	{
		private readonly SafeTimer m_RepeatTimer;
		private readonly SafeCriticalSection m_RepeatSection;

        [CanBeNull]
		private IVolumePoint m_VolumePoint;
		private bool m_Up;
		private DateTime m_Timeout;

		/// <summary>
		/// Tracking the last set level/percentage to mitigate any rounding errors.
		/// </summary>
		private float? m_LastLevel;
		private float? m_LastPercent;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public VolumeRepeater()
		{
			m_RepeatTimer = SafeTimer.Stopped(RepeatTimerCallback);
			m_RepeatSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			m_RepeatTimer.Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts/continues ramping in the given direction.
		/// </summary>
		/// <param name="volumePoint"></param>
		/// <param name="up"></param>
		public void VolumeHold([NotNull] IVolumePoint volumePoint, bool up)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumeHold(volumePoint, up, long.MaxValue);
		}

		public void VolumeHold([NotNull] IVolumePoint volumePoint, bool up, long timeout)
		{
			VolumeHold(volumePoint, up, timeout, false);
		}

		/// <summary>
		/// Starts/continues ramping in the given direction.
		/// Ramping will stop automatically after the given timeout(ms) unless called periodically.
		/// Starts with subsequent ramp values if startSubsequentRamp is true
		/// </summary>
		/// <param name="volumePoint"></param>
		/// <param name="up"></param>
		/// <param name="timeout"></param>
		/// <param name="startSubsequentRamp"></param>
		public void VolumeHold([NotNull] IVolumePoint volumePoint, bool up, long timeout, bool startSubsequentRamp)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			try
			{
				m_Timeout = IcdEnvironment.GetUtcTime().AddMilliseconds(timeout);
			}
			catch (ArgumentOutOfRangeException)
			{
				m_Timeout = DateTime.MaxValue;
			}

			// No change, just needed to update the timeout
			if (volumePoint == m_VolumePoint && up == m_Up)
				return;

			m_VolumePoint = volumePoint;
			m_Up = up;
			m_LastLevel = null;
			m_LastPercent = null;

			if (startSubsequentRamp)
				SubsequentRamp();
			else
				InitialRamp();

			m_RepeatTimer.Reset(m_VolumePoint.VolumeRampInitialInterval, m_VolumePoint.VolumeRampInterval);
		}

		/// <summary>
		/// Starts/continues ramping up.
		/// </summary>
		/// <param name="volumePoint"></param>
		public void VolumeUpHold([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumeHold(volumePoint, true);
		}

		/// <summary>
		/// Starts/continues ramping up.
		/// Ramping will stop automatically after the given timeout(ms) unless called periodically.
		/// </summary>
		/// <param name="volumePoint"></param>
		/// <param name="timeout"></param>
		public void VolumeUpHold([NotNull] IVolumePoint volumePoint, long timeout)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumeHold(volumePoint, true, timeout);
		}

		/// <summary>
		/// Starts/continues ramping down.
		/// </summary>
		/// <param name="volumePoint"></param>
		public void VolumeDownHold([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumeHold(volumePoint, false);
		}

		/// <summary>
		/// Starts/continues ramping down.
		/// Ramping will stop automatically after the given timeout(ms) unless called periodically.
		/// </summary>
		/// <param name="volumePoint"></param>
		/// <param name="timeout"></param>
		public void VolumeDownHold([NotNull] IVolumePoint volumePoint, long timeout)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumeHold(volumePoint, false, timeout);
		}

		/// <summary>
		/// Stops any active ramping.
		/// </summary>
		public void Release()
		{
			m_RepeatSection.Enter();

			try
			{
				m_RepeatTimer.Stop();
				m_VolumePoint = null;
				m_LastLevel = null;
				m_LastPercent = null;
			}
			finally
			{
				m_RepeatSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called for each timer interval.
		/// </summary>
		private void RepeatTimerCallback()
		{
			m_RepeatSection.Enter();

			try
			{
				if (IcdEnvironment.GetUtcTime() > m_Timeout)
					Release();
				else
					SubsequentRamp();
			}
			finally
			{
				m_RepeatSection.Leave();
			}
		}

		/// <summary>
		/// Performs the initial ramp step in the current direction.
		/// </summary>
		private void InitialRamp()
		{
			m_RepeatSection.Enter();

			try
			{
				if (m_VolumePoint == null)
					return;

				switch (m_VolumePoint.VolumeRepresentation)
				{
					case eVolumeRepresentation.Level:
						Ramp(m_VolumePoint.VolumeRampInitialStepSize);
						break;
					case eVolumeRepresentation.Percent:
						Ramp(m_VolumePoint.VolumeRampInitialStepSize / 100);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			finally
			{
				m_RepeatSection.Leave();
			}
		}

		/// <summary>
		/// Performs the subsequent ramp step in the current direction.
		/// </summary>
		private void SubsequentRamp()
		{
			m_RepeatSection.Enter();

			try
			{
				if (m_VolumePoint == null)
					return;

				switch (m_VolumePoint.VolumeRepresentation)
				{
					case eVolumeRepresentation.Level:
						Ramp(m_VolumePoint.VolumeRampStepSize);
						break;
					case eVolumeRepresentation.Percent:
						Ramp(m_VolumePoint.VolumeRampStepSize / 100);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			finally
			{
				m_RepeatSection.Leave();
			}
		}

		/// <summary>
		/// Performs the ramp step in the current direction.
		/// </summary>
		/// <param name="stepSize"></param>
		private void Ramp(float stepSize)
		{
			m_RepeatSection.Enter();

			try
			{
				if (!m_Up)
					stepSize *= -1;

				IVolumeDeviceControl volumeControl = m_VolumePoint == null ? null : m_VolumePoint.Control;
				if (volumeControl == null)
					return;

				if (!volumeControl.SupportedVolumeFeatures.HasFlag(eVolumeFeatures.VolumeAssignment))
					throw new InvalidOperationException("Unable to assign volume to " + volumeControl);

				if (volumeControl.IsMuted)
					volumeControl.SetIsMuted(false);

				float? safetyMin = m_VolumePoint.VolumeSafetyMin;
				float? safetyMax = m_VolumePoint.VolumeSafetyMax;

				switch (m_VolumePoint.VolumeRepresentation)
				{
					case eVolumeRepresentation.Level:

						// Clamp safetyMin and safetyMax to the absolute min/max on the control
						safetyMin = safetyMin.HasValue
							            ? Math.Max(safetyMin.Value, volumeControl.VolumeLevelMin)
							            : volumeControl.VolumeLevelMin;
						safetyMax = safetyMax.HasValue
							            ? Math.Max(safetyMax.Value, volumeControl.VolumeLevelMax)
							            : volumeControl.VolumeLevelMax;

						m_LastLevel = (m_LastLevel ?? volumeControl.VolumeLevel) + stepSize;

						m_LastLevel = MathUtils.Clamp(m_LastLevel.Value, safetyMin.Value, safetyMax.Value);

						volumeControl.SetVolumeLevel(m_LastLevel.Value);
						break;

					case eVolumeRepresentation.Percent:
						m_LastPercent = (m_LastPercent ?? volumeControl.GetVolumePercent()) + stepSize;

						float? safetyMinPercent = safetyMin == null ? (float?)null : volumeControl.ConvertLevelToPercent(safetyMin.Value);
						float? safetyMaxPercent = safetyMax == null ? (float?)null : volumeControl.ConvertLevelToPercent(safetyMax.Value);

						if (safetyMinPercent.HasValue && m_LastPercent < safetyMinPercent)
							m_LastPercent = safetyMinPercent.Value;
						if (safetyMaxPercent.HasValue && m_LastPercent > safetyMaxPercent)
							m_LastPercent = safetyMaxPercent.Value;

						volumeControl.SetVolumePercent(m_LastPercent.Value);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			finally
			{
				m_RepeatSection.Leave();
			}
		}

		#endregion
	}
}
