using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner
{
    public sealed class RoomCombinerSource : AbstractAttributeChild<RoomCombinerBlock>
    {
	    private const string SOURCE_LABEL_ATTRIBUTE = "sourceLabel";

	    [PublicAPI]
		public event EventHandler<StringEventArgs> OnLabelChanged;

	    private string m_Label;

	    #region Properties

	    [PublicAPI]
		public string Label
        {
            get { return m_Label; }
            private set
            {
                if (m_Label == value)
                    return;

                m_Label = value;

                Log(eSeverity.Informational, "Source Label set to {0}", m_Label);

                OnLabelChanged.Raise(this, new StringEventArgs(m_Label));
            }
        }

	    /// <summary>
	    /// Gets the name of the index, used with logging.
	    /// </summary>
	    protected override string IndexName { get { return "source"; } }

	    #endregion

        #region Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        public RoomCombinerSource(RoomCombinerBlock parent, int index)
			: base(parent, index)
        {
            if (Device.Initialized)
                Initialize();
        }

	    public override void Initialize()
        {
            base.Initialize();

            RequestAttribute(LabelFeedback, AttributeCode.eCommand.Get, SOURCE_LABEL_ATTRIBUTE, null, Index);
        }

        [PublicAPI]
        public void SetLabel(string label)
        {
            RequestAttribute(LabelFeedback, AttributeCode.eCommand.Set, SOURCE_LABEL_ATTRIBUTE, new Value(label), Index);
        }

        #endregion

        #region Private Methods

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
