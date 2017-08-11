using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner
{
    public sealed class RoomCombinerSource : AbstractAttributeChild<RoomCombinerBlock>
    {
        #region Properties

        private const string SOURCE_LABEL_ATTRIBUTE = "sourceSelection";
        [PublicAPI]
        public event EventHandler<IntEventArgs> OnSourceLabelChanged;

        private int m_SourceLabel;

        [PublicAPI]
        public int SourceLabel
        {
            get { return m_SourceLabel; }
            private set
            {
                if (m_SourceLabel == value)
                    return;
                m_SourceLabel = value;
                Log(eSeverity.Informational, "Source Label set to {0}", m_SourceLabel);
                OnSourceLabelChanged.Raise(this, new IntEventArgs(m_SourceLabel));
            }
        }


        #endregion
        #region Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        public RoomCombinerSource(RoomCombinerBlock parent, int index) : base(parent, index)
        {
            if (Device.Initialized)
                Initialize();
        }

        /// <summary>
        /// Gets the name of the index, used with logging.
        /// </summary>
        protected override string IndexName { get { return "source"; } }

        public override void Initialize()
        {
            base.Initialize();

            RequestAttribute(SourceLabelFeedback, AttributeCode.eCommand.Get, SOURCE_LABEL_ATTRIBUTE, null, Index);
        }

        [PublicAPI]
        void SetSourceLabel(int source)
        {
            RequestAttribute(SourceLabelFeedback, AttributeCode.eCommand.Set, SOURCE_LABEL_ATTRIBUTE, new Value(source), Index);
        }

        #endregion

        #region Private Methods

        private void SourceLabelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            SourceLabel = innerValue.IntValue;
        }

        #endregion
    }
}