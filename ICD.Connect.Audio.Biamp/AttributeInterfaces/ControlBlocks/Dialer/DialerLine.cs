using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks.Dialer
{
	public sealed class DialerLine : AbstractAttributeChild<DialerBlock>
	{
		private const string DTMF_SERVICE = "dtmf";

		private const string AUTO_ANSWER_ATTRIBUTE = "autoAnswer";
		private const string LAST_NUMBER_DIALED_ATTRIBUTE = "lastNum";
		private const string LINE_LABEL_ATTRIBUTE = "lineLabel";

		private const string SPEED_DIAL_LABEL_ATTRIBUTE = "speedDialLabel";
		private const string SPEED_DIAL_NUMBER_ATTRIBUTE = "speedDialNum";

		private readonly Dictionary<int, DialerCallAppearance> m_CallAppearances;
		private readonly SafeCriticalSection m_CallAppearancesSection;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnLastNumberChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnLineLabelChanged; 

		private bool m_AutoAnswer;
		private string m_LastNumber;
		private string m_LineLabel;

		#region Properties

		[PublicAPI]
		public int CallAppearanceCount { get { return 6; } }

		[PublicAPI]
		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				Log(eSeverity.Informational, "AutoAnswer set to {0}", m_AutoAnswer);

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		[PublicAPI]
		public string LastNumber
		{
			get { return m_LastNumber; }
			private set
			{
				if (value == m_LastNumber)
					return;

				m_LastNumber = value;

				Log(eSeverity.Informational, "LastNumber set to {0}", m_LastNumber);

				OnLastNumberChanged.Raise(this, new StringEventArgs(m_LastNumber));
			}
		}

		[PublicAPI]
		public string LineLabel
		{
			get { return m_LineLabel; }
			private set
			{
				if (value == m_LineLabel)
					return;

				m_LineLabel = value;

				Log(eSeverity.Informational, "LineLabel set to {0}", m_LineLabel);

				OnLineLabelChanged.Raise(this, new StringEventArgs(m_LineLabel));
			}
		}


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Line"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public DialerLine(DialerBlock parent, int index)
			: base(parent, index)
		{
			m_CallAppearances = new Dictionary<int, DialerCallAppearance>();
			m_CallAppearancesSection = new SafeCriticalSection();

			if (Device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			DisposeCallAppearances();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			RebuildCallAppearances();

			base.Initialize();

			// Get initial values
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Get, AUTO_ANSWER_ATTRIBUTE, null, Index);
			RequestAttribute(LastNumberDialedFeedback, AttributeCode.eCommand.Get, LAST_NUMBER_DIALED_ATTRIBUTE, null, Index);
			RequestAttribute(LineLabelFeedback, AttributeCode.eCommand.Get, LINE_LABEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			// Subscribe
			RequestAttribute(AutoAnswerFeedback, command, AUTO_ANSWER_ATTRIBUTE, null, Index);
			RequestAttribute(LastNumberDialedFeedback, command, LAST_NUMBER_DIALED_ATTRIBUTE, null, Index);
			RequestAttribute(LineLabelFeedback, command, LINE_LABEL_ATTRIBUTE, null, Index);		
		}

		[PublicAPI]
		public IEnumerable<DialerCallAppearance> GetCallAppearances()
		{
			return m_CallAppearancesSection.Execute(() => m_CallAppearances.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Sends the DTMF tone to the given call. Number must be in the range 0-9 or * or #.
		/// Used only when off-hook.
		/// </summary>
		/// <param name="key"></param>
		[PublicAPI]
		public void Dtmf(char key)
		{
			RequestService(DTMF_SERVICE, new Value(key), Index);
		}

		[PublicAPI]
		public void SetAutoAnswer(bool enabled)
		{
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Set, AUTO_ANSWER_ATTRIBUTE, new Value(enabled), Index);
		}

		[PublicAPI]
		public void ToggleAutoAnswer()
		{
			RequestAttribute(AutoAnswerFeedback, AttributeCode.eCommand.Toggle, AUTO_ANSWER_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing call appearances and rebuilds from call appearance count.
		/// </summary>
		private void RebuildCallAppearances()
		{
			m_CallAppearancesSection.Enter();

			try
			{
				Enumerable.Range(1, CallAppearanceCount).ForEach(i => LazyLoadCallAppearance(i));
			}
			finally
			{
				m_CallAppearancesSection.Leave();
			}
		}

		/// <summary>
		/// Gets the call appearance at the given index. Creates it if it doesn't exist.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private DialerCallAppearance LazyLoadCallAppearance(int index)
		{
			m_CallAppearancesSection.Enter();

			try
			{
				if (!m_CallAppearances.ContainsKey(index))
					m_CallAppearances[index] = new DialerCallAppearance(this, index);
				return m_CallAppearances[index];
			}
			finally
			{
				m_CallAppearancesSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing call appearances.
		/// </summary>
		private void DisposeCallAppearances()
		{
			m_CallAppearancesSection.Enter();

			try
			{
				m_CallAppearances.Values.ForEach(c => c.Dispose());
				m_CallAppearances.Clear();
			}
			finally
			{
				m_CallAppearancesSection.Leave();
			}
		}

		#endregion

		#region Subscription Feedback

		private void AutoAnswerFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			AutoAnswer = innerValue.BoolValue;
		}

		private void LastNumberDialedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LastNumber = innerValue.StringValue;
		}

		private void LineLabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LineLabel = innerValue.StringValue;
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

			addRow("Call Appearance Count", CallAppearanceCount);
			addRow("Auto Answer", AutoAnswer);
			addRow("Last Number", LastNumber);
			addRow("Line Label", LineLabel);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<char>("Dtmf", "Dtmf <DIGIT>", c => Dtmf(c));
			yield return new GenericConsoleCommand<bool>("SetAutoAnswer", "SetAutoAnswer <true/false>", b => SetAutoAnswer(b));
			yield return new ConsoleCommand("ToggleAutoAnswer", "", () => ToggleAutoAnswer());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("CallAppearances", GetCallAppearances(), c => (uint)c.Index);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
