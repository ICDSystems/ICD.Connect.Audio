using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.Services;
using ICD.Connect.Audio.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Audio.Biamp.Tesira
{
	public sealed class BiampExternalTelemetryProvider : AbstractExternalTelemetryProvider<BiampTesiraDevice>
	{
		private const string NO_FAULTS_MESSAGE = "No fault in device";

		#region Events

		[EventTelemetry(DspTelemetryNames.ACTIVE_FAULT_STATE_CHANGED)]
		public event EventHandler<BoolEventArgs> OnActiveFaultStateChanged;
		
		[EventTelemetry(DspTelemetryNames.ACTIVE_FAULT_MESSAGE_CHANGED)]
		public event EventHandler<StringEventArgs> OnActiveFaultMessagesChanged;

		#endregion

		#region Fields

		private bool m_ActiveFaultState;
		private string m_ActiveFaultMessages;

		#endregion

		#region Properties

		[PropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_STATE, null, DspTelemetryNames.ACTIVE_FAULT_STATE_CHANGED)]
		public bool ActiveFaultState
		{
			get { return m_ActiveFaultState; }
			private set
			{
				if (m_ActiveFaultState == value)
					return;

				m_ActiveFaultState = value;

				OnActiveFaultStateChanged.Raise(this, new BoolEventArgs(m_ActiveFaultState));
			}
		}

		[PropertyTelemetry(DspTelemetryNames.ACTIVE_FAULT_MESSAGE, null, DspTelemetryNames.ACTIVE_FAULT_MESSAGE_CHANGED)]
		public string ActiveFaultMessages
		{
			get { return m_ActiveFaultMessages; } 
			private set
			{
				if (m_ActiveFaultMessages == value)
					return;

				m_ActiveFaultMessages = value;

				OnActiveFaultMessagesChanged.Raise(this, new StringEventArgs(m_ActiveFaultMessages));
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(BiampTesiraDevice parent)
		{
			base.SetParent(parent);

			Update();
		}

		private void Update()
		{
			DeviceService service = Parent == null ? null : GetDeviceService(Parent);
			if (service == null)
				return;

			// Active Faults
			string faults = service.ActiveFaultStatus;
			if (string.IsNullOrEmpty(faults) || string.Equals(faults, NO_FAULTS_MESSAGE, StringComparison.OrdinalIgnoreCase))
			{
				ActiveFaultState = false;
				ActiveFaultMessages = string.Empty;
				return;
			}
			ActiveFaultState = true;
			ActiveFaultMessages = faults;

		}

		private static DeviceService GetDeviceService(BiampTesiraDevice parent)
		{
			return parent.AttributeInterfaces.LazyLoadService<DeviceService>();
		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(BiampTesiraDevice parent)
		{
			base.Subscribe(parent);

			DeviceService service = parent == null ? null : GetDeviceService(parent);
			if (service == null)
				return;

			service.OnFaultStatusChanged += ParentOnFaultStatusChanged;
		}

		protected override void Unsubscribe(BiampTesiraDevice parent)
		{
			base.Unsubscribe(parent);

			DeviceService service = parent == null ? null : GetDeviceService(parent);
			if (service == null)
				return;

			service.OnFaultStatusChanged -= ParentOnFaultStatusChanged;
		}

		private void ParentOnFaultStatusChanged(object sender, StringEventArgs args)
		{
			string faults = args.Data;

			if (string.IsNullOrEmpty(faults) || string.Equals(faults, NO_FAULTS_MESSAGE, StringComparison.OrdinalIgnoreCase))
			{
				ActiveFaultState = false;
				ActiveFaultMessages = string.Empty;
				return;
			}
			ActiveFaultState = true;
			ActiveFaultMessages = faults;
		}

		#endregion
	}
}