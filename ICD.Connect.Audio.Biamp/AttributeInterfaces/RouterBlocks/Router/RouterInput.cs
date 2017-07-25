using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.Router
{
	public sealed class RouterInput : AbstractAttributeChild<RouterBlock>
	{
		private const string LABEL_ATTRIBUTE = "inputLabel";

		public event EventHandler<StringEventArgs> OnLabelChanged;

		private string m_Label;

		[PublicAPI]
		public string Label
		{
			get { return m_Label; }
			private set
			{
				if (value == m_Label)
					return;

				m_Label = value;

				Log(eSeverity.Informational, "Label set to {0}", m_Label);

				OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
			}
		}


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "Input"; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public RouterInput(RouterBlock parent, int index)
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
			OnLabelChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, LABEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the label of the output.
		/// </summary>
		/// <param name="label"></param>
		[PublicAPI]
		public void SetLabel(string label)
		{
			RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, LABEL_ATTRIBUTE, new Value(label), Index);
		}

		#endregion

		#region Subscription Callbacks

		private void LabelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Label = innerValue.StringValue;
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

			addRow("Label", Label);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("SetLabel", "SetLabel <LABEL>", s => SetLabel(s));
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
