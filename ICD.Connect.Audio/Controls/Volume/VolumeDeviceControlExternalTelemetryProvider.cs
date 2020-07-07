using System;
using ICD.Common.Utils.Attributes;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Audio.Controls.Volume
{
	public sealed class VolumeDeviceControlExternalTelemetryProvider : AbstractExternalTelemetryProvider<IVolumeDeviceControl>
	{
		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_LEVEL_CHANGED)]
		public event EventHandler<FloatEventArgs> OnVolumeLevelChanged;

		[EventTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_PERCENT_CHANGED)]
		public event EventHandler<FloatEventArgs> OnVolumePercentChanged;

		private float m_VolumeLevel;
		private float m_VolumePercent;

		#region Properties

		[PropertyTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_LEVEL,
			VolumeTelemetryNames.VOLUME_CONTROL_LEVEL_COMMAND,
			VolumeTelemetryNames.VOLUME_CONTROL_LEVEL_CHANGED)]
		public float VolumeLevel
		{
			get { return m_VolumeLevel; }
			private set
			{
				if (Math.Abs(value - m_VolumeLevel) < 0.001f)
					return;

				m_VolumeLevel = value;

				OnVolumeLevelChanged.Raise(this, new FloatEventArgs(m_VolumeLevel));
			}
		}

		[PropertyTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_PERCENT,
			VolumeTelemetryNames.VOLUME_CONTROL_PERCENT_COMMAND,
			VolumeTelemetryNames.VOLUME_CONTROL_PERCENT_CHANGED)]
		[Range(0.0f, 1.0f)]
		public float VolumePercent
		{
			get { return m_VolumePercent; }
			private set
			{
				if (Math.Abs(value - m_VolumePercent) < 0.001f)
					return;

				m_VolumePercent = value;

				OnVolumePercentChanged.Raise(this, new FloatEventArgs(m_VolumePercent));
			}
		}

		#endregion

		#region Methods

		[MethodTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_LEVEL_COMMAND)]
		public void SetVolumeLevel(float level)
		{
			Parent.SetVolumeLevel(level);
		}

		[MethodTelemetry(VolumeTelemetryNames.VOLUME_CONTROL_PERCENT_COMMAND)]
		public void SetVolumePercent([Range(0.0f, 1.0f)] float percent)
		{
			Parent.SetVolumePercent(percent);
		}

		#endregion

		#region Private Methods

		private void Update()
		{
			UpdateLevel();
			UpdatePercent();
		}

		private void UpdateLevel()
		{
			VolumeLevel = Parent == null ? 0 : Parent.VolumeLevel;
		}

		private void UpdatePercent()
		{
			VolumePercent = Parent == null ? 0 : Parent.GetVolumePercent();
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(IVolumeDeviceControl parent)
		{
			base.SetParent(parent);

			Update();
		}

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(IVolumeDeviceControl parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			parent.OnVolumeChanged += ParentOnVolumeChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IVolumeDeviceControl parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			parent.OnVolumeChanged += ParentOnVolumeChanged;
		}

		private void ParentOnVolumeChanged(object sender, VolumeControlVolumeChangedApiEventArgs eventArgs)
		{
			UpdateLevel();
			UpdatePercent();
		}

		#endregion
	}
}
