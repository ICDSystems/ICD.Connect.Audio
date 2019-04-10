using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.Services;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Audio.Biamp
{
	public class BiampExternalTelemetryProvider : IBiampExternalTelemetryProvider
	{
		public event EventHandler OnRequestTelemetryRebuild;
		public event EventHandler<BoolEventArgs> OnVoipRegisteredChanged;
		public event EventHandler<BoolEventArgs> OnActiveFaultsChanged;
		public event EventHandler<BoolEventArgs> OnAtcCallActiveChanged;
		public event EventHandler<StringEventArgs> OnFirmwareVersionChanged;
		public event EventHandler<StringEventArgs> OnVoipRegistrationStatusChanged;

		private BiampTesiraDevice m_Parent;
		private bool m_VoipRegistered;
		private bool m_ActiveFaults;
		private bool m_AtcCallActive;
		private string m_FirmwareVersion;
		private string m_VoipRegistrationStatus;

		public void SetParent(ITelemetryProvider provider)
		{
			if (!(provider is BiampTesiraDevice))
				throw new InvalidOperationException(
					string.Format("Cannot create external telemetry for provider {0}, " +
								  "Provider must be of type BiampTesiraDevice.", provider));

			if (m_Parent != null)
			{
				foreach (var line in GetVoipProviders().SelectMany(b => b.GetLines()))
				{
					line.OnRegistrationStatusChanged -= LineOnRegistrationStatusChanged;
				}
				foreach (var callControl in m_Parent.Controls.OfType<IDialingDeviceControl>())
				{
					callControl.OnSourceAdded -= ParentOnSourceAddedOrRemoved;
					callControl.OnSourceRemoved -= ParentOnSourceAddedOrRemoved;
					callControl.OnSourceChanged -= ParentOnSourceAddedOrRemoved;
				}
				GetDeviceService().OnFirmwareVersionChanged -= ParentOnFirmwareVersionChanged;
				GetDeviceService().OnFaultStatusChanged -= ParentOnFaultStatusChanged;
			}

			m_Parent = (BiampTesiraDevice)provider;

			if (m_Parent != null)
			{
				foreach (var line in GetVoipProviders().SelectMany(b => b.GetLines()))
				{
					line.OnRegistrationStatusChanged += LineOnRegistrationStatusChanged;
				}
				foreach (var callControl in m_Parent.Controls.OfType<IDialingDeviceControl>())
				{
					callControl.OnSourceAdded += ParentOnSourceAddedOrRemoved;
					callControl.OnSourceRemoved += ParentOnSourceAddedOrRemoved;
					callControl.OnSourceChanged += ParentOnSourceAddedOrRemoved;
				}
				GetDeviceService().OnRegistrationChanged += ParentOnFirmwareVersionChanged;
				GetDeviceService().OnFaultStatusChanged += ParentOnFaultStatusChanged;
			}
		}

		private IEnumerable<VoIpControlStatusBlock> GetVoipProviders()
		{
			return m_Parent.AttributeInterfaces.GetAttributeInterfaces().OfType<VoIpControlStatusBlock>();
		}

		public bool VoipRegistered
		{
			get { return m_VoipRegistered; }
			private set
			{
				if (m_VoipRegistered == value)
					return;

				m_VoipRegistered = value;

				OnVoipRegisteredChanged.Raise(this, new BoolEventArgs(m_VoipRegistered));
			}
		}

		public bool ActiveFaults
		{
			get { return m_ActiveFaults; }
			private set
			{
				if (m_ActiveFaults == value)
					return;

				m_ActiveFaults = value;

				OnActiveFaultsChanged.Raise(this, new BoolEventArgs(m_ActiveFaults));
			}
		}

		public bool AtcCallActive
		{
			get { return m_AtcCallActive; }
			private set
			{
				if (m_AtcCallActive == value)
					return;

				m_AtcCallActive = value;

				OnAtcCallActiveChanged.Raise(this, new BoolEventArgs(m_AtcCallActive));
			}
		}

		public string FirmwareVersion
		{
			get { return m_FirmwareVersion; }
			private set
			{
				if (m_FirmwareVersion == value)
					return;

				m_FirmwareVersion = value;

				OnFirmwareVersionChanged.Raise(this, new StringEventArgs(m_FirmwareVersion));
			}
		}

		public string VoipRegistrationStatus
		{
			get { return m_VoipRegistrationStatus; }
			private set
			{
				if (m_VoipRegistrationStatus == value)
					return;

				m_VoipRegistrationStatus = value;

				OnVoipRegistrationStatusChanged.Raise(this, new StringEventArgs(m_VoipRegistrationStatus));
			}
		}

		private DeviceService GetDeviceService()
		{
			return m_Parent.AttributeInterfaces.GetAttributeInterface<DeviceService>(DeviceService.INSTANCE_TAG);
		}

		private void LineOnRegistrationStatusChanged(VoIpControlStatusLine sender, VoIpControlStatusLine.eRegistrationStatus status)
		{
			VoipRegistrationStatus = status.ToString();
			VoipRegistered = status == VoIpControlStatusLine.eRegistrationStatus.VoipRegistered;
		}

		private void ParentOnFaultStatusChanged(object sender, StringEventArgs args)
		{
			ActiveFaults = args.Data != string.Empty;
		}

		private void ParentOnFirmwareVersionChanged(object sender, StringEventArgs args)
		{
			FirmwareVersion = args.Data;
		}

		private void ParentOnSourceAddedOrRemoved(object sender, ConferenceSourceEventArgs conferenceSourceEventArgs)
		{
			AtcCallActive = m_Parent.Controls.OfType<IDialingDeviceControl>().SelectMany(c => c.GetSources()).Any();
		}
	}

	public interface IBiampExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[EventTelemetry(DspTelemetryNames.VOIP_REGISTERED_CHANGED)]
		event EventHandler<BoolEventArgs> OnVoipRegisteredChanged;

		[EventTelemetry(DspTelemetryNames.ACTIVE_FAULTS_CHANGED)]
		event EventHandler<BoolEventArgs> OnActiveFaultsChanged;

		[EventTelemetry(DspTelemetryNames.CALL_ACTIVE_CHANGED)]
		event EventHandler<BoolEventArgs> OnAtcCallActiveChanged; 

		[EventTelemetry(DspTelemetryNames.FIRMWARE_VERSION_CHANGED)]
		event EventHandler<StringEventArgs> OnFirmwareVersionChanged;

		[EventTelemetry(DspTelemetryNames.VOIP_REGISTRATION_STATUS_CHANGED)]
		event EventHandler<StringEventArgs> OnVoipRegistrationStatusChanged; 

		[DynamicPropertyTelemetry(DspTelemetryNames.VOIP_REGISTERED, DspTelemetryNames.VOIP_REGISTERED_CHANGED)]
		bool VoipRegistered { get; }

		[DynamicPropertyTelemetry(DspTelemetryNames.ACTIVE_FAULTS, DspTelemetryNames.ACTIVE_FAULTS_CHANGED)]
		bool ActiveFaults { get; }

		[DynamicPropertyTelemetry(DspTelemetryNames.CALL_ACTIVE, DspTelemetryNames.CALL_ACTIVE_CHANGED)]
		bool AtcCallActive { get; }

		[DynamicPropertyTelemetry(DspTelemetryNames.FIRMWARE_VERSION, DspTelemetryNames.FIRMWARE_VERSION_CHANGED)]
		string FirmwareVersion { get; }

		[DynamicPropertyTelemetry(DspTelemetryNames.VOIP_REGISTRATION_STATUS, DspTelemetryNames.VOIP_REGISTRATION_STATUS_CHANGED)]
		string VoipRegistrationStatus { get; }
	}
}