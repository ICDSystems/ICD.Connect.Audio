using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks.Dialer
{
	[PublicAPI]
	public sealed class DialerBlock : AbstractControlBlock
	{
		private const string CALL_STATE_ATTRIBUTE = "callState";
		private const string DISPLAY_NAME_LABEL_ATTRIBUTE = "displayNameLabel";
		private const string LINE_COUNT_ATTRIBUTE = "numChannels";
		
		/// <summary>
		/// Raised when the display name label changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnDisplayNameLabelChanged;

		/// <summary>
		/// Raised when the line count changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnLineCountChanged;

		private readonly Dictionary<int, DialerLine> m_Lines;
		private readonly SafeCriticalSection m_LinesSection;

		private string m_DisplayNameLabel;
		private int m_LineCount;

		#region Properties

		/// <summary>
		/// Gets the last cached display name label.
		/// </summary>
		[PublicAPI]
		public string DisplayNameLabel
		{
			get { return m_DisplayNameLabel; }
			private set
			{
				if (value == m_DisplayNameLabel)
					return;

				m_DisplayNameLabel = value;

				Log(eSeverity.Informational, "DisplayNameLabel set to {0}", m_DisplayNameLabel);

				OnDisplayNameLabelChanged.Raise(this, new StringEventArgs(m_DisplayNameLabel));
			}
		}

		/// <summary>
		/// Gets the last cached line count.
		/// </summary>
		[PublicAPI]
		public int LineCount
		{
			get { return m_LineCount; }
			private set
			{
				if (value == m_LineCount)
					return;

				m_LineCount = value;

				Log(eSeverity.Informational, "LineCount set to {0}", m_LineCount);

				RebuildLines();

				OnLineCountChanged.Raise(this, new IntEventArgs(m_LineCount));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public DialerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Lines = new Dictionary<int, DialerLine>();
			m_LinesSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnDisplayNameLabelChanged = null;
			OnLineCountChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Unsubscribe, CALL_STATE_ATTRIBUTE, null);

			DisposeLines();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Get, CALL_STATE_ATTRIBUTE, null);
			RequestAttribute(DisplayNameLabelFeedback, AttributeCode.eCommand.Get, DISPLAY_NAME_LABEL_ATTRIBUTE, null);
			RequestAttribute(LineCountFeedback, AttributeCode.eCommand.Get, LINE_COUNT_ATTRIBUTE, null);

			// Subscribe
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Subscribe, CALL_STATE_ATTRIBUTE, null);
		}

		[PublicAPI]
		public IEnumerable<DialerLine> GetLines()
		{
			return m_LinesSection.Execute(() => m_Lines.OrderValuesByKey().ToArray());
		}

			/// <summary>
		/// Sets the display name label on the biamp.
		/// </summary>
		/// <param name="label"></param>
		[PublicAPI]
		public void SetDisplayNameLabel(string label)
		{
			RequestAttribute(DisplayNameLabelFeedback, AttributeCode.eCommand.Set, DISPLAY_NAME_LABEL_ATTRIBUTE, new Value(label));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing lines and rebuilds from line count.
		/// </summary>
		private void RebuildLines()
		{
			m_LinesSection.Enter();

			try
			{
				Enumerable.Range(1, LineCount).ForEach(i => LazyLoadLine(i));
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		/// <summary>
		/// Gets the channel at the given index. If the channel doesn't exist, creates a new one.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private DialerLine LazyLoadLine(int index)
		{
			m_LinesSection.Enter();

			try
			{
				if (!m_Lines.ContainsKey(index))
					m_Lines[index] = new DialerLine(this, index);
				return m_Lines[index];
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing lines.
		/// </summary>
		private void DisposeLines()
		{
			m_LinesSection.Enter();

			try
			{
				m_Lines.Values.ForEach(c => c.Dispose());
				m_Lines.Clear();
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		private void CallStateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void DisplayNameLabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			DisplayNameLabel = innerValue.StringValue;
		}

		private void LineCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LineCount = innerValue.IntValue;
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

			addRow("Display Name Label", DisplayNameLabel);
			addRow("Line Count", LineCount);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("SetDisplayName", "SetDisplayName <LABEL>", s => SetDisplayNameLabel(s));
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

			yield return ConsoleNodeGroup.KeyNodeMap("Lines", GetLines(), l => (uint)l.Index);
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
