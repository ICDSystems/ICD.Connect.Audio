using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Mock.Devices
{
	public sealed class MockAudioDeviceVolumeControl : AbstractVolumeLevelDeviceControl<IDeviceBase>, IVolumeMuteFeedbackDeviceControl
	{
		private bool m_VolumeIsMuted;
		private float m_VolumeLevel;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted
		{
			get { return m_VolumeIsMuted; }
			set
			{
				if (value == m_VolumeIsMuted)
					return;
				
				m_VolumeIsMuted = value;

				Log(eSeverity.Informational, "VolumeIsMuted changed to {0}", m_VolumeIsMuted);

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_VolumeIsMuted));
			}
		}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public override float VolumeLevel { get { return m_VolumeLevel; } }

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		protected override float VolumeRawMaxAbsolute { get { return 100; } }

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		protected override float VolumeRawMinAbsolute { get { return 0; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public MockAudioDeviceVolumeControl(IDeviceBase parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnMuteStateChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			VolumeIsMuted = !VolumeIsMuted;
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			VolumeIsMuted = mute;
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeLevel(float volume)
		{
			volume = MathUtils.Clamp(volume, VolumeRawMinAbsolute, VolumeRawMaxAbsolute);

			if (Math.Abs(volume - m_VolumeLevel) < 0.0001f)
				return;

			m_VolumeLevel = volume;

			Log(eSeverity.Informational, "VolumeRaw changed to {0}", m_VolumeLevel);

			VolumeFeedback(m_VolumeLevel);
		}

		#endregion
	}
}
