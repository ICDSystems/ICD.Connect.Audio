using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks.LogicState;
using ICD.Connect.Audio.Misc.BiColorMicLed;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public sealed class ParleMicLedsDevice : AbstractDevice<ParleMicLedsDeviceSettings>, IBiColorMicLed
	{
		#region Events

		/// <summary>
		/// Raised when the parent Biamp device changes.
		/// </summary>
		public event EventHandler OnBiampChanged;

		public event EventHandler<BoolEventArgs> OnPowerEnabledChanged;
		public event EventHandler<BoolEventArgs> OnRedLedEnabledChanged;
		public event EventHandler<BoolEventArgs> OnGreenLedEnabledChanged;

		#endregion

		private const int POWER_LOGIC_STATE_CHANNEL = 1;
		private const int COLOR_LOGIC_STATE_CHANNEL = 2;

		[CanBeNull]
		private BiampTesiraDevice m_Biamp;

		[CanBeNull]
		private LogicStateBlock m_LogicStateBlock;

		private bool m_PowerEnabled;
		private bool m_RedLedEnabled;
		private bool m_GreenLedEnabled;

		#region Properties

		/// <summary>
		/// Gets the enabled state of the power output.
		/// </summary>
		[PublicAPI]
		public bool PowerEnabled
		{
			get { return m_PowerEnabled; }
			private set
			{
				if (value == m_PowerEnabled)
					return;

				m_PowerEnabled = value;

				Logger.LogSetTo(eSeverity.Informational, "PowerEnabled", m_PowerEnabled);

				OnPowerEnabledChanged.Raise(this, new BoolEventArgs(m_PowerEnabled));
			}
		}

		/// <summary>
		/// Gets the enabled state of the red LED.
		/// </summary>
		[PublicAPI]
		public bool RedLedEnabled
		{
			get { return m_RedLedEnabled; }
			private set
			{
				if (value == m_RedLedEnabled)
					return;

				m_RedLedEnabled = value;

				Logger.LogSetTo(eSeverity.Informational, "RedLedEnabled", m_RedLedEnabled);

				OnRedLedEnabledChanged.Raise(this, new BoolEventArgs(m_RedLedEnabled));
			}
		}

		/// <summary>
		/// Gets the enabled state of the green LED.
		/// </summary>
		[PublicAPI]
		public bool GreenLedEnabled
		{
			get { return m_GreenLedEnabled; }
			private set
			{
				if (value == m_GreenLedEnabled)
					return;

				m_GreenLedEnabled = value;

				Logger.LogSetTo(eSeverity.Informational, "GreenLedEnabled", m_GreenLedEnabled);

				OnGreenLedEnabledChanged.Raise(this, new BoolEventArgs(m_GreenLedEnabled));
			}
		}

		#endregion

		#region Methods

		public void SetPowerEnabled(bool enabled)
		{
			if (m_LogicStateBlock == null)
				throw new InvalidOperationException();

			m_LogicStateBlock.GetChannel(POWER_LOGIC_STATE_CHANNEL).SetState(enabled);
		}

		public void SetRedLedEnabled(bool enabled)
		{
			if (m_LogicStateBlock == null)
				throw new InvalidOperationException();

			// Red state is false on the logic block so we flip the value here.
			bool redLedState = !enabled;

			m_LogicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).SetState(redLedState);
		}

		public void SetGreenLedEnabled(bool enabled)
		{
			if (m_LogicStateBlock == null)
				throw new InvalidOperationException();

			// Green state is true on the logic block so we maintain the value.
			bool greenLedState = enabled;

			m_LogicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).SetState(greenLedState);
		}

		private void UpdatePowerState()
		{
			if (m_LogicStateBlock != null)
				PowerEnabled = m_LogicStateBlock.GetChannel(POWER_LOGIC_STATE_CHANNEL).State;
		}

		private void UpdateRedLedState()
		{
			if (m_LogicStateBlock != null)
				PowerEnabled = !m_LogicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).State;
		}

		private void UpdateGreenLedState()
		{
			if (m_LogicStateBlock != null)
				PowerEnabled = m_LogicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).State;
		}

		#endregion

		#region Device Base

		protected override void DisposeFinal(bool disposing)
		{
			OnBiampChanged = null;
			OnPowerEnabledChanged = null;
			OnRedLedEnabledChanged = null;
			OnGreenLedEnabledChanged = null;

			base.DisposeFinal(disposing);
		}

		protected override bool GetIsOnlineStatus()
		{
			return m_Biamp != null && m_Biamp.IsOnline;
		}

		#endregion

		#region Settings

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetBiamp(null, null);
		}

		protected override void CopySettingsFinal(ParleMicLedsDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.BiampId = m_Biamp == null ? null : (int?)m_Biamp.Id;
			settings.InstanceTag = m_LogicStateBlock == null ? null : m_LogicStateBlock.InstanceTag;
		}

		protected override void ApplySettingsFinal(ParleMicLedsDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			BiampTesiraDevice biamp = null;

			if (settings.BiampId.HasValue)
			{
				try
				{
					biamp = factory.GetOriginatorById<BiampTesiraDevice>(settings.BiampId.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Biamp Tesira Device with id {0}", settings.BiampId.Value);
				}
			}

			SetBiamp(biamp, settings.InstanceTag);
		}

		[PublicAPI]
		public void SetBiamp(BiampTesiraDevice biamp, string instanceTag)
		{
			Unsubscribe(m_Biamp);
			m_Biamp = biamp;
			Subscribe(m_Biamp);

			Unsubscribe(m_LogicStateBlock);
			m_LogicStateBlock = m_Biamp == null || instanceTag == null
				                    ? null
				                    : m_Biamp.AttributeInterfaces
				                             .LazyLoadAttributeInterface<LogicStateBlock>(instanceTag);
			Subscribe(m_LogicStateBlock);

			UpdateCachedOnlineStatus();
			OnBiampChanged.Raise(this);
		}

		#endregion

		#region Biamp Callbacks

		private void Subscribe(BiampTesiraDevice biamp)
		{
			if (biamp == null)
				return;

			biamp.OnIsOnlineStateChanged += BiampOnOnIsOnlineStateChanged;
		}

		private void Unsubscribe(BiampTesiraDevice biamp)
		{
			if (biamp == null)
				return;

			biamp.OnIsOnlineStateChanged -= BiampOnOnIsOnlineStateChanged;
		}

		private void BiampOnOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Logic Block Callbacks

		private void Subscribe(LogicStateBlock logicStateBlock)
		{
			if (logicStateBlock == null)
				return;

			logicStateBlock.GetChannel(POWER_LOGIC_STATE_CHANNEL).OnStateChanged += OnPowerStateChanged;
			logicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).OnStateChanged += OnColorStateChanged;
		}

		private void Unsubscribe(LogicStateBlock logicStateBlock)
		{
			if (logicStateBlock == null)
				return;

			logicStateBlock.GetChannel(POWER_LOGIC_STATE_CHANNEL).OnStateChanged -= OnPowerStateChanged;
			logicStateBlock.GetChannel(COLOR_LOGIC_STATE_CHANNEL).OnStateChanged -= OnColorStateChanged;
		}

		private void OnPowerStateChanged(object sender, BoolEventArgs e)
		{
			UpdatePowerState();
		}

		private void OnColorStateChanged(object sender, BoolEventArgs e)
		{
			UpdateRedLedState();
			UpdateGreenLedState();
		}

		#endregion
	}
}