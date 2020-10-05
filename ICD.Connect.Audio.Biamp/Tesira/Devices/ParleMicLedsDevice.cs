using System;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.LogicBlocks.LogicState;
using ICD.Connect.Audio.Misc.BiColorMicLed;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public sealed class ParleMicLedsDevice : AbstractTesiraChildAttributeInterfaceDevice<LogicStateBlock, ParleMicLedsDeviceSettings>, IBiColorMicLed
	{
		#region Events

		public event EventHandler<BoolEventArgs> OnPowerEnabledChanged;
		public event EventHandler<BoolEventArgs> OnRedLedEnabledChanged;
		public event EventHandler<BoolEventArgs> OnGreenLedEnabledChanged;

		#endregion

		private const int POWER_LOGIC_STATE_CHANNEL = 1;
		private const int COLOR_LOGIC_STATE_CHANNEL = 2;

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
			if (AttributeInterface == null)
				throw new InvalidOperationException();

			AttributeInterface.GetChannel(POWER_LOGIC_STATE_CHANNEL).SetState(enabled);
		}

		public void SetRedLedEnabled(bool enabled)
		{
			if (AttributeInterface == null)
				throw new InvalidOperationException();

			// Red state is false on the logic block so we flip the value here.
			bool redLedState = !enabled;

			AttributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).SetState(redLedState);
		}

		public void SetGreenLedEnabled(bool enabled)
		{
			if (AttributeInterface == null)
				throw new InvalidOperationException();

			// Green state is true on the logic block so we maintain the value.
			bool greenLedState = enabled;

			AttributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).SetState(greenLedState);
		}

		private void UpdatePowerState()
		{
			if (AttributeInterface != null)
				PowerEnabled = AttributeInterface.GetChannel(POWER_LOGIC_STATE_CHANNEL).State;
		}

		private void UpdateRedLedState()
		{
			if (AttributeInterface != null)
				PowerEnabled = !AttributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).State;
		}

		private void UpdateGreenLedState()
		{
			if (AttributeInterface != null)
				PowerEnabled = AttributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).State;
		}

		#endregion

		#region Device Base

		protected override void DisposeFinal(bool disposing)
		{
			OnPowerEnabledChanged = null;
			OnRedLedEnabledChanged = null;
			OnGreenLedEnabledChanged = null;

			base.DisposeFinal(disposing);
		}

		#endregion

		#region Logic Block Callbacks

		protected override void Subscribe(LogicStateBlock attributeInterface)
		{
			base.Subscribe(attributeInterface);

			if (attributeInterface == null)
				return;

			attributeInterface.GetChannel(POWER_LOGIC_STATE_CHANNEL).OnStateChanged += LogicStateBlockOnPowerStateChanged;
			attributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).OnStateChanged += LogicStateBlockOnColorStateChanged;
		}

		protected override void Unsubscribe(LogicStateBlock attributeInterface)
		{
			base.Unsubscribe(attributeInterface);

			if (attributeInterface == null)
				return;

			attributeInterface.GetChannel(POWER_LOGIC_STATE_CHANNEL).OnStateChanged -= LogicStateBlockOnPowerStateChanged;
			attributeInterface.GetChannel(COLOR_LOGIC_STATE_CHANNEL).OnStateChanged -= LogicStateBlockOnColorStateChanged;
		}

		private void LogicStateBlockOnPowerStateChanged(object sender, BoolEventArgs e)
		{
			UpdatePowerState();
		}

		private void LogicStateBlockOnColorStateChanged(object sender, BoolEventArgs e)
		{
			UpdateRedLedState();
			UpdateGreenLedState();
		}

		#endregion
	}
}