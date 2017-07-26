using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp
{
	public sealed class VoIpControlStatusCallAppearance : AbstractAttributeChild<VoIpControlStatusLine>
	{
		public enum eVoIpCallState
		{
			Init,
			Fault,
			Idle,
			DialTone,
			Silent,
			Dialing,
			RingBack,
			Ringing,
			Busy,
			Reject,
			InvalidNumber,
			Active,
			ActiveMuted,
			Hold,
			WaitingRing,
			ConfActive,
			ConfHold,
			AnswerCall
		}

		public enum eVoIpPrompt
		{
			None,
			Starting,
			Registering,
			SipUserNotConfigured,
			EnterNumber,
			Connecting,
			IncomingCallFrom,
			PeerBusy,
			CallCannotBeCompleted,
			Hold,
			Held,
			Conference,
			ConferenceOnHold,
			Connected,
			ConnectedMuted,
			AuthFailure,
			ProxyNotConfigured,
			NetworkInit,
			DhcpInProgress,
			NetworkLinkDown,
			NetworkLinkUp,
			IpAddressConflict,
			NetworkConfigured,
			CodecNegotiationFailure,
			UnexpectedError,
			AuthUserNotConfigured,
			AuthPasswordNotConfigured
		}

		private static readonly Dictionary<string, eVoIpCallState> s_CallStateSerials =
			new Dictionary<string, eVoIpCallState>(StringComparer.OrdinalIgnoreCase)
			{
				{"VOIP_CALL_STATE_INIT", eVoIpCallState.Init},
				{"VOIP_CALL_STATE_FAULT", eVoIpCallState.Fault},
				{"VOIP_CALL_STATE_IDLE", eVoIpCallState.Idle},
				{"VOIP_CALL_STATE_DIALTONE", eVoIpCallState.DialTone},
				{"VOIP_CALL_STATE_SILENT", eVoIpCallState.Silent},
				{"VOIP_CALL_STATE_DIALING", eVoIpCallState.Dialing},
				{"VOIP_CALL_STATE_RINGBACK", eVoIpCallState.RingBack},
				{"VOIP_CALL_STATE_RINGING", eVoIpCallState.Ringing},
				{"VOIP_CALL_STATE_BUSY", eVoIpCallState.Busy},
				{"VOIP_CALL_STATE_REJECT", eVoIpCallState.Reject},
				{"VOIP_CALL_STATE_INVALID_NUMBER", eVoIpCallState.InvalidNumber},
				{"VOIP_CALL_STATE_ACTIVE", eVoIpCallState.Active},
				{"VOIP_CALL_STATE_ACTIVE_MUTED", eVoIpCallState.ActiveMuted},
				{"VOIP_CALL_STATE_ON_HOLD", eVoIpCallState.Hold},
				{"VOIP_CALL_STATE_WAITING_RING", eVoIpCallState.WaitingRing},
				{"VOIP_CALL_STATE_CONF_ACTIVE", eVoIpCallState.ConfActive},
				{"VOIP_CALL_STATE_CONF_HOLD", eVoIpCallState.ConfHold},
				{"VOIP_CALL_STATE_ANSWER_CALL", eVoIpCallState.AnswerCall},
			};

		private static readonly Dictionary<string, eVoIpPrompt> s_PromptSerials =
			new Dictionary<string, eVoIpPrompt>(StringComparer.OrdinalIgnoreCase)
			{
				{"VOIP_PROMPT_NONE", eVoIpPrompt.None},
				{"VOIP_PROMPT_STARTING", eVoIpPrompt.Starting},
				{"VOIP_PROMPT_REGISTERING", eVoIpPrompt.Registering},
				{"VOIP_PROMPT_SIP_USER_NOT_CONFIGURED", eVoIpPrompt.SipUserNotConfigured},
				{"VOIP_PROMPT_ENTER_NUMBER", eVoIpPrompt.EnterNumber},
				{"VOIP_PROMPT_CONNECTING", eVoIpPrompt.Connecting},
				{"VOIP_PROMPT_INCOMING_CALL_FROM", eVoIpPrompt.IncomingCallFrom},
				{"VOIP_PROMPT_PEER_BUSY", eVoIpPrompt.PeerBusy},
				{"VOIP_PROMPT_CALL_CANNOT_BE_COMPLETED", eVoIpPrompt.CallCannotBeCompleted},
				{"VOIP_PROMPT_ON_HOLD", eVoIpPrompt.Hold},
				{"VOIP_PROMPT_CALL_ON_HELD", eVoIpPrompt.Held},
				{"VOIP_PROMPT_CONFERENCE", eVoIpPrompt.Conference},
				{"VOIP_PROMPT_CONFERENCE_ON_HOLD", eVoIpPrompt.ConferenceOnHold},
				{"VOIP_PROMPT_CONNECTED", eVoIpPrompt.Connected},
				{"VOIP_PROMPT_CONNECTED_MUTED", eVoIpPrompt.ConnectedMuted},
				{"VOIP_PROMPT_AUTH_FAILURE", eVoIpPrompt.AuthFailure},
				{"VOIP_PROMPT_PROXY_NOT_CONFIGURED", eVoIpPrompt.ProxyNotConfigured},
				{"VOIP_PROMPT_NETWORK_INIT", eVoIpPrompt.NetworkInit},
				{"VOIP_PROMPT_DHCP_IN_PROGRESS", eVoIpPrompt.DhcpInProgress},
				{"VOIP_PROMPT_NETWORK_LINK_DOWN", eVoIpPrompt.NetworkLinkDown},
				{"VOIP_PROMPT_NETWORK_LINK_UP", eVoIpPrompt.NetworkLinkUp},
				{"VOIP_PROMPT_IPADDR_CONFLICT", eVoIpPrompt.IpAddressConflict},
				{"VOIP_PROMPT_NETWORK_CONFIGURED", eVoIpPrompt.NetworkConfigured},
				{"VOIP_PROMPT_CODEC_NEGOTIATION_FAILURE", eVoIpPrompt.CodecNegotiationFailure},
				{"VOIP_PROMPT_UNEXPECTED_ERROR", eVoIpPrompt.UnexpectedError},
				{"VOIP_PROMPT_AUTH_USER_NOT_CONFIGURED", eVoIpPrompt.AuthUserNotConfigured},
				{"VOIP_PROMPT_AUTH_PASSWORD_NOT_CONFIGURED", eVoIpPrompt.AuthPasswordNotConfigured},
			}; 

		private const string REDIAL_SERVICE = "redial";
		private const string END_SERVICE = "end";
		private const string FLASH_SERVICE = "flash";
		private const string SEND_SERVICE = "send";
		private const string DIAL_SERVICE = "dial";
		private const string ANSWER_SERVICE = "answer";
		private const string CONFERENCE_SERVICE = "lconf";
		private const string RESUME_SERVICE = "resume";
		private const string LEAVE_CONFERENCE_SERVICE = "leaveConf";
		private const string HOLD_SERVICE = "hold";
		private const string GO_OFF_HOOK_SERVICE = "offHook";
		private const string GO_ON_HOOK_SERVICE = "onHook";

		private const string SIMPLE_CALLER_ID_ATTRIBUTE = "cid";
		private const string FULL_CALLER_ID_ATTRIBUTE = "cidUser";
		private const string LINE_IN_USE_ATTRIBUTE = "lineInUse";
		private const string RINGING_ATTRIBUTE = "ringing";

		public delegate void PromptCallback(VoIpControlStatusCallAppearance callAppearance, eVoIpPrompt prompt);

		public delegate void CallStateCallback(VoIpControlStatusCallAppearance callAppearance, eVoIpCallState state);

		[PublicAPI]
		public event PromptCallback OnPromptChanged;

		[PublicAPI]
		public event CallStateCallback OnCallStateChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnLineInUseChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnCallerNumberChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnCallerNameChanged;

		private bool m_LineInUse;
		private eVoIpPrompt m_Prompt;
		private eVoIpCallState m_State;
		private string m_CallerNumber;
		private string m_CallerName;

		#region Properties

		private int Line { get { return Parent.Index; } }

		[PublicAPI]
		public bool LineInUse
		{
			get { return m_LineInUse; }
			private set
			{
				if (value == m_LineInUse)
					return;

				m_LineInUse = value;

				Log(eSeverity.Informational, "LineInUse set to {0}", m_LineInUse);

				OnLineInUseChanged.Raise(this, new BoolEventArgs(m_LineInUse));
			}
		}

		[PublicAPI]
		public eVoIpPrompt Prompt
		{
			get { return m_Prompt; }
			private set
			{
				if (value == m_Prompt)
					return;

				m_Prompt = value;

				Log(eSeverity.Informational, "Prompt set to {0}", m_Prompt);

				PromptCallback handler = OnPromptChanged;
				if (handler != null)
					handler(this, m_Prompt);
			}
		}

		[PublicAPI]
		public eVoIpCallState State
		{
			get { return m_State; }
			private set
			{
				if (value == m_State)
					return;

				m_State = value;

				Log(eSeverity.Informational, "State set to {0}", m_State);

				CallStateCallback handler = OnCallStateChanged;
				if (handler != null)
					handler(this, m_State);
			}
		}

		[PublicAPI]
		public string CallerNumber
		{
			get { return m_CallerNumber; }
			private set
			{
				if (value == m_CallerNumber)
					return;

				m_CallerNumber = value;

				Log(eSeverity.Informational, "CallerNumber set to {0}", m_CallerNumber);

				OnCallerNumberChanged.Raise(this, new StringEventArgs(m_CallerNumber));
			}
		}

		[PublicAPI]
		public string CallerName
		{
			get { return m_CallerName; }
			private set
			{
				if (value == m_CallerName)
					return;

				m_CallerName = value;

				Log(eSeverity.Informational, "CallerName set to {0}", m_CallerName);

				OnCallerNameChanged.Raise(this, new StringEventArgs(m_CallerName));
			}
		}


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "CallAppearance"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public VoIpControlStatusCallAppearance(VoIpControlStatusLine parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnPromptChanged = null;
			OnCallStateChanged = null;
			OnLineInUseChanged = null;
			OnCallerNumberChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Unsubscribe, LINE_IN_USE_ATTRIBUTE, null, Line, Index);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Get, LINE_IN_USE_ATTRIBUTE, null, Line, Index);

			// Subscribe
			RequestAttribute(LineInUseFeedback, AttributeCode.eCommand.Subscribe, LINE_IN_USE_ATTRIBUTE, null, Line, Index);
		}

		internal void ParseCallState(ControlValue callState)
		{
			Value stateValue = callState.GetValue<Value>("state");
			State = stateValue.GetObjectValue(s_CallStateSerials);

			Value promptValue = callState.GetValue<Value>("prompt");
			Prompt = promptValue.GetObjectValue(s_PromptSerials);

			Value cidValue = callState.GetValue<Value>("cid");
			string[] cidSplit = cidValue.GetStringValues().ToArray();

			// First portion is datetime
			CallerNumber = cidSplit.Length > 2 ? cidSplit[1] : null;
			CallerName = cidSplit.Length > 3 ? cidSplit[2] : null;
		}

		#region Services

		[PublicAPI]
		public void Redial()
		{
			RequestService(REDIAL_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void End()
		{
			RequestService(END_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Flash()
		{
			RequestService(FLASH_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Send()
		{
			RequestService(SEND_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Dial(string number)
		{
			RequestService(DIAL_SERVICE, new Value(number), Line, Index);
		}

		[PublicAPI]
		public void Answer()
		{
			RequestService(ANSWER_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Conference()
		{
			RequestService(CONFERENCE_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Resume()
		{
			RequestService(RESUME_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void LeaveConference()
		{
			RequestService(LEAVE_CONFERENCE_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Hold()
		{
			RequestService(HOLD_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void GoOffHook()
		{
			RequestService(GO_OFF_HOOK_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void GoOnHook()
		{
			RequestService(GO_ON_HOOK_SERVICE, null, Line, Index);
		}

		#endregion

		#endregion

		#region Private Methods

		private void LineInUseFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LineInUse = innerValue.BoolValue;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Line", Line);
			addRow("Line In Use", LineInUse);
			addRow("Prompt", Prompt);
			addRow("State", State);
			addRow("Active Number", CallerNumber);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Redial", "", () => Redial());
			yield return new ConsoleCommand("End", "", () => End());
			yield return new ConsoleCommand("Flash", "", () => Flash());
			yield return new ConsoleCommand("Send", "", () => Send());
			yield return new GenericConsoleCommand<string>("Dial", "Dial <NUMBER>", s => Dial(s));
			yield return new ConsoleCommand("Answer", "", () => Answer());
			yield return new ConsoleCommand("Conference", "", () => Conference());
			yield return new ConsoleCommand("LeaveConference", "", () => LeaveConference());
			yield return new ConsoleCommand("Hold", "", () => Hold());
			yield return new ConsoleCommand("GoOffHook", "", () => GoOffHook());
			yield return new ConsoleCommand("GoOnHook", "", () => GoOnHook());
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
