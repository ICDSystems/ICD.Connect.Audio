using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControls;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.QSys.Controls
{
	public sealed class VoipComponentDialingDevice : AbstractDialingDeviceControl<QSysCoreDevice>, IQSysKrangControl
	{

		private const float TOLERANCE = (float)(0.0001);

		#region Fields
		private BooleanNamedControl m_HoldControl;

		private BooleanNamedControl m_PrivacyMuteControl;

		private VoipNamedComponent m_VoipComponent;

		private ThinConferenceSource m_ConferenceSource;

		private readonly SafeCriticalSection m_ConferenceSourceCriticalSection;
		#endregion


		public VoipComponentDialingDevice(int id, string friendlyName, CoreElementsLoadContext context, string xml) : base(context.QSysCore, id)
		{
			m_ConferenceSourceCriticalSection = new SafeCriticalSection();


		    string voipName = XmlUtils.TryReadChildElementContentAsString(xml, "ControlName");
		    string privacyMuteName = XmlUtils.TryReadChildElementContentAsString(xml, "PrivacyMuteControl");
		    string holdName = XmlUtils.TryReadChildElementContentAsString(xml, "HoldControl");

			//If we don't have names defined for the controls, bail out
		    if (String.IsNullOrEmpty(voipName))
			    throw new
				    InvalidOperationException(String.Format("Tried to create VoipComponentDialingDevice {0}:{1} without VoIPComponentName",
				                                            id, friendlyName));
			if (String.IsNullOrEmpty(privacyMuteName))
				throw new
					InvalidOperationException(String.Format("Tried to create VoipComponentDialingDevice {0}:{1} without PrivacyMuteControlName",
					                                        id, friendlyName));

		    if (String.IsNullOrEmpty(holdName))
			    throw new
				    InvalidOperationException(String.Format("Tried to create VoipComponentDialingDevice {0}:{1} without HoldControlName",
				                                            id, friendlyName));

			// Load volume/mute controls
		    m_VoipComponent = context.LazyLoadNamedComponent(voipName, typeof(VoipNamedComponent)) as VoipNamedComponent;
		    m_PrivacyMuteControl = context.LazyLoadNamedControl(privacyMuteName, typeof(BooleanNamedControl)) as BooleanNamedControl;
			m_HoldControl = context.LazyLoadNamedControl(holdName, typeof(BooleanNamedControl)) as BooleanNamedControl;

		    if (m_VoipComponent == null)
			    throw new KeyNotFoundException(String.Format("QSys - No VoIP Component {0}", voipName));
		    if (m_PrivacyMuteControl == null)
			    throw new KeyNotFoundException(String.Format("QSys - No Privacy Mute Control {0}", privacyMuteName));
		    if (m_HoldControl == null)
			    throw new KeyNotFoundException(String.Format("QSys - No Hold Control {0}", holdName));

		    Subscribe();
	    }

		#region Events

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		#endregion

		#region Properties

		private ThinConferenceSource ConferenceSource
		{
			get
			{
				ThinConferenceSource returnSource = null;
				m_ConferenceSourceCriticalSection.Execute(() => returnSource = m_ConferenceSource);
				return returnSource;
			}
			set
			{
				m_ConferenceSourceCriticalSection.Enter();
				try
				{
					if (m_ConferenceSource != null)
					{
						OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(m_ConferenceSource));
						Unsubscribe(m_ConferenceSource);
					}

					m_ConferenceSource = value;

					if (m_ConferenceSource != null)
					{
						Subscribe(m_ConferenceSource);
						OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(m_ConferenceSource));
					}
				}
				finally
				{
					m_ConferenceSourceCriticalSection.Leave();
				}

			}
		}

		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Audio; } }

		#endregion

		#region Public Methods

		public override IEnumerable<IConferenceSource> GetSources()
		{
			if (ConferenceSource != null)
				yield return ConferenceSource;
			else
				yield break;
		}

		public override void Dial(string number)
		{
			Dial(number, eConferenceSourceType.Audio);
		}

		public override void Dial(string number, eConferenceSourceType callType)
		{
			if (callType == eConferenceSourceType.Video)
				throw new ArgumentException(String.Format("VoIPDialingDevice {0} does not support dialing video calls", this));

			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_NUMBER).SetValue(number);
			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_CONNECT).TriggerControl();

		}

		public override void SetDoNotDisturb(bool enabled)
		{
			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_DND).SetValue(enabled ? "1" : "0");
		}

		public override void SetAutoAnswer(bool enabled)
		{
			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_AUTOANSWER).SetValue(enabled ? "1" : "0");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			m_PrivacyMuteControl.SetValue(enabled);
		}

		#endregion

		#region PrivateMethods

		private void Subscribe()
		{
			m_VoipComponent.OnControlValueUpdated += VoipComponentControlValueUpdated;
			m_PrivacyMuteControl.OnValueUpdated += PrivacyMuteControlOnValueUpdated;
			m_HoldControl.OnValueUpdated += HoldControlOnValueUpdated;
		}

		private void Unsubscribe()
		{
			m_VoipComponent.OnControlValueUpdated -= VoipComponentControlValueUpdated;
			m_PrivacyMuteControl.OnValueUpdated -= PrivacyMuteControlOnValueUpdated;
			m_HoldControl.OnValueUpdated -= HoldControlOnValueUpdated;
		}

		private void Subscribe(ThinConferenceSource conferenceSource)
		{
			if (m_ConferenceSource == null)
				return;

			conferenceSource.AnswerCallback += ConferenceSourceAnswerCallback;
			conferenceSource.HangupCallback += ConferenceSourceHangupCallback;
			conferenceSource.HoldCallback += ConferenceSourceHoldCallback;
			conferenceSource.ResumeCallback += ConferenceSourceResumeCallback;
			conferenceSource.SendDtmfCallback += ConferenceSourceSendDtmfCallback;
		}

		private void Unsubscribe(ThinConferenceSource conferenceSource)
		{
			if (conferenceSource == null)
				return;
			conferenceSource.AnswerCallback = null;
			conferenceSource.HangupCallback = null;
			conferenceSource.HoldCallback = null;
			conferenceSource.ResumeCallback = null;
			conferenceSource.SendDtmfCallback = null;

			
		}

		private void ConferenceSourceSendDtmfCallback(ThinConferenceSource sender, string dtmf)
		{
			// todo: bail if not off hook?
			string controlName;
			switch (dtmf)
			{
				case "0":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_0;
					break;
				case "1":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_1;
					break;
				case "2":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_2;
					break;
				case "3":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_3;
					break;
				case "4":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_4;
					break;
				case "5":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_5;
					break;
				case "6":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_6;
					break;
				case "7":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_7;
					break;
				case "8":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_8;
					break;
				case "9":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_9;
					break;
				case "*":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_STAR;
					break;
				case "#":
					controlName = VoipNamedComponent.CONTROL_CALL_PAD_POUND;
					break;
				default:
					throw new ArgumentException(string.Format("VoIP Dialing Device {0} - DTMF code {1} not supported", this, dtmf));
						break;
			}
			m_VoipComponent.GetControl(controlName).TriggerControl();
		}

		private void ConferenceSourceResumeCallback(ThinConferenceSource sender)
		{
			m_HoldControl.SetValue(false);
		}

		private void ConferenceSourceHoldCallback(ThinConferenceSource sender)
		{
			//todo: Verify call is in a state to hold?
			m_HoldControl.SetValue(true);
		}

		private void ConferenceSourceHangupCallback(ThinConferenceSource sender)
		{
			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_DISCONNECT).TriggerControl();
		}

		private void ConferenceSourceAnswerCallback(ThinConferenceSource sender)
		{
			m_VoipComponent.GetControl(VoipNamedComponent.CONTROL_CALL_CONNECT).TriggerControl();
		}

		private void VoipComponentControlValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			INamedComponentControl control = sender as INamedComponentControl;
			if (control == null)
				throw new
					InvalidOperationException(String.Format("VoIP Dialing Device {0}:{1} - VoipComponentOnControlValueUpdated sender isn't an INamedComponentControl",
					                          Id, Name));

			switch (control.Name)
			{
				case VoipNamedComponent.CONTROL_CALL_STATUS:
					ParseCallStatus(e);
					break;
				case VoipNamedComponent.CONTROL_CALL_AUTOANSWER:
					ParseAutoAnswer(e);
					break;
				case VoipNamedComponent.CONTROL_CALL_DND:
					ParseDnd(e);
					break;
				case VoipNamedComponent.CONTROL_CALL_CID_NAME:
					ParseCidName(e);
					break;
				case VoipNamedComponent.CONTROL_CALL_CID_NUMBER:
					ParseCidNumber(e);
					break;

			}

			
		}

		private void ParseDnd(ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			DoNotDisturb = Math.Abs(controlValueUpdateEventArgs.ValuePosition) > TOLERANCE;
		}

		private void ParseCidNumber(ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			if (!String.IsNullOrEmpty(controlValueUpdateEventArgs.ValueString))
				GetOrCreateConferenceSource().Number = controlValueUpdateEventArgs.ValueString;
		}

		private void ParseCidName(ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			if (!String.IsNullOrEmpty(controlValueUpdateEventArgs.ValueString))
				GetOrCreateConferenceSource().Name = controlValueUpdateEventArgs.ValueString;
		}

		private void ParseAutoAnswer(ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			AutoAnswer = Math.Abs(controlValueUpdateEventArgs.ValuePosition) > TOLERANCE;
		}

		private void ParseCallStatus(ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			Parent.Log(eSeverity.Debug, "Call Status: {0}", controlValueUpdateEventArgs.ValueString);
			eConferenceSourceStatus callStatus = QSysStatusToConferenceSourceStatus(controlValueUpdateEventArgs.ValueString);

			if (callStatus == eConferenceSourceStatus.Disconnected | callStatus == eConferenceSourceStatus.Idle)
			{
				ThinConferenceSource source = ConferenceSource;
				if (source == null)
					return;
				source.Status = callStatus;
				source.End = DateTime.Now;
				DistroyConferenceSource();
				return;
			}

			GetOrCreateConferenceSource().Status = callStatus;
			if (callStatus == eConferenceSourceStatus.Ringing)
				GetOrCreateConferenceSource().Direction = eConferenceSourceDirection.Incoming;
			if (callStatus == eConferenceSourceStatus.Dialing)
				GetOrCreateConferenceSource().Direction = eConferenceSourceDirection.Outgoing;
		}

		private void PrivacyMuteControlOnValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			PrivacyMuted = Math.Abs(e.ValueRaw) > TOLERANCE;
		}

		private void HoldControlOnValueUpdated(object sender, ControlValueUpdateEventArgs e)
		{
			bool onHold = Math.Abs(e.ValueRaw) > TOLERANCE;

			ThinConferenceSource source = ConferenceSource;
			if (source == null)
				return;

			if (source.Status == eConferenceSourceStatus.Connected && onHold)
				source.Status = eConferenceSourceStatus.OnHold;
			else if (source.Status == eConferenceSourceStatus.OnHold && !onHold)
				source.Status = eConferenceSourceStatus.Connected;
		}

		private ThinConferenceSource GetOrCreateConferenceSource()
		{
			ThinConferenceSource source;
			m_ConferenceSourceCriticalSection.Enter();
			try
			{
				source = ConferenceSource;
				if (source == null)
				{
					source = new ThinConferenceSource();
					ConferenceSource = source;
				}

				return source;
			}
			finally
			{
				m_ConferenceSourceCriticalSection.Leave();
			}
		}

		private void DistroyConferenceSource()
		{
			ConferenceSource = null;
		}

		private static eConferenceSourceStatus QSysStatusToConferenceSourceStatus(string qsysStatus)
		{
			string status = qsysStatus.Split(new Char[] {'-'}, 2)[0].Trim();

			switch (status.ToLower())
			{
				case "idle":
					return eConferenceSourceStatus.Disconnected;
				case "normal clearing":
					return eConferenceSourceStatus.Disconnected;
				case "disconnected":
					return eConferenceSourceStatus.Disconnected;
				case "dialing":
					return eConferenceSourceStatus.Dialing;
				case "connected":
					return eConferenceSourceStatus.Connected;
				case "incoming call":
					return eConferenceSourceStatus.Ringing;
				default:
					return eConferenceSourceStatus.Undefined;
			}

		}


		#endregion
	}
}
