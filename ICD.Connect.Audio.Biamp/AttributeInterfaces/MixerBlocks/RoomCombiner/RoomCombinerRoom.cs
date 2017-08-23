using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.API.Nodes;
using ICD.Connect.API.Commands;
using System.Collections.Generic;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner
{
    public sealed class RoomCombinerRoom : AbstractAttributeChild<RoomCombinerBlock>
    {

        #region Properties
        private const string ROOM_GROUP_ATTRIBUTE = "group";
        private const string INPUT_LEVEL_ATTRIBUTE = "levelIn";
        private const string MAX_INPUT_LEVEL_ATTRIBUTE = "levelInMax";
        private const string MIN_INPUT_LEVEL_ATTRIBUTE = "levelInMin";
        private const string OUTPUT_LEVEL_ATTRIBUTE = "levelOut";
        private const string MAX_OUTPUT_LEVEL_ATTRIBUTE = "levelOutMax";
        private const string MIN_OUTPUT_LEVEL_ATTRIBUTE = "levelOutMin";
        private const string SOURCE_LEVEL_ATTRIBUTE = "levelSource";
        private const string MAX_SOURCE_LEVEL_ATTRIBUTE = "levelSourceMax";
        private const string MIN_SOURCE_LEVEL_ATTRIBUTE = "levelSourceMin";
        private const string INPUT_MUTE_ATTRIBUTE = "muteIn";
        private const string OUTPUT_MUTE_ATTRIBUTE = "muteOut";
        private const string SOURCE_MUTE_ATTRIBUTE = "muteSource";
        private const string SOURCE_SELECTION_ATTRIBUTE = "sourceSelection";

        [PublicAPI]
        public event EventHandler<IntEventArgs> OnRoomGroupChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnInputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMaxInputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMinInputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnOutputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMaxOutputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMinOutputLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnSourceLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMaxSourceLevelChanged;
        [PublicAPI]
        public event EventHandler<FloatEventArgs> OnMinSourceLevelChanged;
        [PublicAPI]
        public event EventHandler<BoolEventArgs> OnInputMuteChanged;
        [PublicAPI]
        public event EventHandler<BoolEventArgs> OnOutputMuteChanged;
        [PublicAPI]
        public event EventHandler<BoolEventArgs> OnSourceMuteChanged;
        [PublicAPI]
        public event EventHandler<IntEventArgs> OnSourceSelectionChanged;

        private int m_RoomGroup;
        private float m_InputLevel;
        private float m_MaxInputLevel;
        private float m_MinInputLevel;
        private float m_OutputLevel;
        private float m_MaxOutputLevel;
        private float m_MinOutputLevel;
        private float m_SourceLevel;
        private float m_MaxSourceLevel;
        private float m_MinSourceLevel;
        private bool m_InputMute;
        private bool m_OutputMute;
        private bool m_SourceMute;
        private int m_SourceSelection;

        [PublicAPI]
        public int RoomGroup
        {
            get { return m_RoomGroup; }
            private set
            {
                if (m_RoomGroup == value)
                    return;
                m_RoomGroup = value;
                Log(eSeverity.Informational, "Room Group set to {0}", m_RoomGroup);
                OnRoomGroupChanged.Raise(this, new IntEventArgs(m_RoomGroup));
            }
        }

        [PublicAPI]
        public float InputLevel
        {
            get { return m_InputLevel; }
            private set
            {
                if (Math.Abs(m_InputLevel - value) < 0.01f)
                    return;
                m_InputLevel = value;
                Log(eSeverity.Informational, "Input Level set to {0}", m_InputLevel);
                OnInputLevelChanged.Raise(this, new FloatEventArgs(m_InputLevel));
            }
        }

        [PublicAPI]
        public float MaxInputLevel
        {
            get { return m_MaxInputLevel; }
            private set
            {
                if (Math.Abs(m_MaxInputLevel - value) < 0.01f)
                    return;

                m_MaxInputLevel = value;
                Log(eSeverity.Informational, "Max Input Level set to {0}", m_MaxInputLevel);
                OnMaxInputLevelChanged.Raise(this, new FloatEventArgs(m_MaxInputLevel));
            }
        }

        [PublicAPI]
        public float MinInputLevel
        {
            get { return m_MinInputLevel; }
            private set
            {
                if (Math.Abs(m_MinInputLevel - value) < 0.01f)
                    return;
                m_MinInputLevel = value;
                Log(eSeverity.Informational, "Min Input Level set to {0}", m_MinInputLevel);
                OnMinInputLevelChanged.Raise(this, new FloatEventArgs(m_MinInputLevel));
            }
        }

        [PublicAPI]
        public float OutputLevel
        {
            get { return m_OutputLevel; }
            private set
            {
                if (Math.Abs(m_OutputLevel - value) < 0.01f)
                    return;
                m_OutputLevel = value;
                Log(eSeverity.Informational, "Output Level set to {0}", m_OutputLevel);
                OnOutputLevelChanged.Raise(this, new FloatEventArgs(m_OutputLevel));
            }
        }

        [PublicAPI]
        public float MaxOutputLevel
        {
            get { return m_MaxOutputLevel; }
            private set
            {
                if (Math.Abs(m_MaxOutputLevel - value) < 0.01f)
                    return;
                m_MaxOutputLevel = value;
                Log(eSeverity.Informational, "Max Output Level set to {0}", m_MaxOutputLevel);
                OnMaxOutputLevelChanged.Raise(this, new FloatEventArgs(m_MaxOutputLevel));
            }
        }

        [PublicAPI]
        public float MinOutputLevel
        {
            get { return m_MinOutputLevel; }
            private set
            {
                if (Math.Abs(m_MinInputLevel - value) < 0.01f)
                    return;
                m_MinOutputLevel = value;
                Log(eSeverity.Informational, "Min Output Level set to {0}", m_MinOutputLevel);
                OnMinOutputLevelChanged.Raise(this, new FloatEventArgs(m_MinOutputLevel));
            }
        }

        [PublicAPI]
        public float SourceLevel
        {
            get { return m_SourceLevel; }
            private set
            {
                if (Math.Abs(m_SourceLevel - value) < 0.01f)
                    return;
                m_SourceLevel = value;
                Log(eSeverity.Informational, "Source Level set to {0}", m_SourceLevel);
                OnSourceLevelChanged.Raise(this, new FloatEventArgs(m_SourceLevel));
            }
        }

        [PublicAPI]
        public float MaxSourceLevel
        {
            get { return m_MaxSourceLevel; }
            private set
            {
                if (Math.Abs(m_MaxSourceLevel - value) < 0.01f)
                    return;
                m_MaxSourceLevel = value;
                Log(eSeverity.Informational, "Max Source Level set to {0}", m_MaxSourceLevel);
                OnMaxSourceLevelChanged.Raise(this, new FloatEventArgs(m_MaxSourceLevel));
            }
        }

        [PublicAPI]
        public float MinSourceLevel
        {
            get { return m_MinSourceLevel; }
            private set
            {
                if (Math.Abs(m_MinSourceLevel - value) < 0.01f)
                    return;
                m_MinSourceLevel = value;
                Log(eSeverity.Informational, "Min Source Level set to {0}", m_MinSourceLevel);
                OnMinSourceLevelChanged.Raise(this, new FloatEventArgs(m_MinSourceLevel));
            }
        }

        [PublicAPI]
        public bool InputMute
        {
            get { return m_InputMute; }
            private set
            {
                if (m_InputMute == value)
                    return;
                m_InputMute = value;
                Log(eSeverity.Informational, "Input Mute set to {0}", m_InputMute);
                OnInputMuteChanged.Raise(this, new BoolEventArgs(m_InputMute));
            }
        }

        [PublicAPI]
        public bool OutputMute
        {
            get { return m_OutputMute; }
            private set
            {
                if (m_OutputMute == value)
                    return;
                m_OutputMute = value;
                Log(eSeverity.Informational, "Output Mute set to {0}", m_OutputMute);
                OnOutputMuteChanged.Raise(this, new BoolEventArgs(m_OutputMute));
            }
        }

        [PublicAPI]
        public bool SourceMute
        {
            get { return m_SourceMute; }
            private set
            {
                if (m_SourceMute == value)
                    return;
                m_SourceMute = value;
                Log(eSeverity.Informational, "Source Mute set to {0}", m_SourceMute);
                OnSourceMuteChanged.Raise(this, new BoolEventArgs(m_SourceMute));
            }
        }

        [PublicAPI]
        public int SourceSelection
        {
            get { return m_SourceSelection; }
            private set
            {
                if (m_SourceSelection == value)
                    return;
                m_SourceSelection = value;
                Log(eSeverity.Informational, "Source Selection set to {0}", m_SourceSelection);
                OnSourceSelectionChanged.Raise(this, new IntEventArgs(m_SourceSelection));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        public RoomCombinerRoom(RoomCombinerBlock parent, int index) : base(parent, index)
        {
            if (Device.Initialized)
                Initialize();
        }

        /// <summary>
        /// Gets the name of the index, used with logging.
        /// </summary>
        protected override string IndexName { get { return "room"; } }

        public override void Initialize()
        {
            base.Initialize();
            RequestAttribute(RoomGroupFeedback, AttributeCode.eCommand.Get, ROOM_GROUP_ATTRIBUTE, null, Index);
            RequestAttribute(InputLevelFeedback, AttributeCode.eCommand.Get, INPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MaxInputLevelFeedback, AttributeCode.eCommand.Get, MAX_INPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MinInputLevelFeedback, AttributeCode.eCommand.Get, MIN_INPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Get, OUTPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Get, MAX_OUTPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Get, MIN_OUTPUT_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(SourceLevelFeedback, AttributeCode.eCommand.Get, SOURCE_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MaxSourceLevelFeedback, AttributeCode.eCommand.Get, MAX_SOURCE_LEVEL_ATTRIBUTE, null, Index);
            RequestAttribute(MinSourceLevelFeedback, AttributeCode.eCommand.Get, MIN_SOURCE_LEVEL_ATTRIBUTE, null, Index);

            RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Get, INPUT_MUTE_ATTRIBUTE, null, Index);
            RequestAttribute(OutputMuteFeedback, AttributeCode.eCommand.Get, OUTPUT_MUTE_ATTRIBUTE, null, Index);
            RequestAttribute(SourceMuteFeedback, AttributeCode.eCommand.Get, SOURCE_MUTE_ATTRIBUTE, null, Index);

            RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Get, SOURCE_SELECTION_ATTRIBUTE, null, Index);
        }

        [PublicAPI]
        public void SetRoom(int id)
        {
            RequestAttribute(RoomGroupFeedback, AttributeCode.eCommand.Set, ROOM_GROUP_ATTRIBUTE, new Value(id), Index);
        }

        [PublicAPI]
        public void SetInputLevel(float level)
        {
            RequestAttribute(InputLevelFeedback, AttributeCode.eCommand.Set, INPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMaxInputLevel(float level)
        {
            RequestAttribute(MaxInputLevelFeedback, AttributeCode.eCommand.Set, MAX_INPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMinInputLevel(float level)
        {
            RequestAttribute(MinInputLevelFeedback, AttributeCode.eCommand.Set, MIN_INPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetOutputLevel(float level)
        {
            RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Set, OUTPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMaxOutputLevel(float level)
        {
            RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Set, MAX_OUTPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMinOutputLevel(float level)
        {
            RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Set, MIN_OUTPUT_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetSourceLevel(float level)
        {
            RequestAttribute(SourceLevelFeedback, AttributeCode.eCommand.Set, SOURCE_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMaxSourceLevel(float level)
        {
            RequestAttribute(MaxSourceLevelFeedback, AttributeCode.eCommand.Set, MAX_SOURCE_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetMinSourceLevel(float level)
        {
            RequestAttribute(MinSourceLevelFeedback, AttributeCode.eCommand.Set, MIN_SOURCE_LEVEL_ATTRIBUTE, new Value(level), Index);
        }

        [PublicAPI]
        public void SetInputMute(bool value)
        {
            RequestAttribute(InputMuteFeedback, AttributeCode.eCommand.Set, INPUT_MUTE_ATTRIBUTE, new Value(value), Index);
        }

        [PublicAPI]
        public void SetOutputMute(bool value)
        {
            RequestAttribute(OutputMuteFeedback, AttributeCode.eCommand.Set, OUTPUT_MUTE_ATTRIBUTE, new Value(value), Index);
        }

        [PublicAPI]
        public void SetSourceMute(bool value)
        {
            RequestAttribute(SourceMuteFeedback, AttributeCode.eCommand.Set, SOURCE_MUTE_ATTRIBUTE, new Value(value), Index);
        }

        [PublicAPI]
        public void SetSourceSelection(int id)
        {
            RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Set, SOURCE_SELECTION_ATTRIBUTE, new Value(id), Index);
        }

        #endregion

        #region Private Methods

        private void RoomGroupFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            RoomGroup = innerValue.IntValue;
        }

        private void InputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            InputLevel = innerValue.FloatValue;
        }

        private void MaxInputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MaxInputLevel = innerValue.FloatValue;
        }

        private void MinInputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MinInputLevel = innerValue.FloatValue;
        }

        private void OutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            OutputLevel = innerValue.FloatValue;
        }

        private void MaxOutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MaxOutputLevel = innerValue.FloatValue;
        }

        private void MinOutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MinOutputLevel = innerValue.FloatValue;
        }

        private void SourceLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            SourceLevel = innerValue.FloatValue;
        }

        private void MaxSourceLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MaxSourceLevel = innerValue.FloatValue;
        }

        private void MinSourceLevelFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            MinSourceLevel = innerValue.FloatValue;
        }

        private void InputMuteFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            InputMute = innerValue.BoolValue;
        }

        private void OutputMuteFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            OutputMute = innerValue.BoolValue;
        }

        private void SourceMuteFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            SourceMute = innerValue.BoolValue;
        }

        private void SourceSelectionFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            SourceSelection = innerValue.IntValue;
        }

        #endregion

        #region Console Commands


        /// <summary>
        /// Calls the delegate for each console status item.
        /// </summary>
        /// <param name="addRow"></param>
        public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
        {
            base.BuildConsoleStatus(addRow);

            addRow("Room Group", RoomGroup);
            addRow("Input Level", InputLevel);
            addRow("Max Input Level", MaxInputLevel);
            addRow("Min Input Level", MinInputLevel);
            addRow("Output Level", OutputLevel);
            addRow("Max Output Level", MaxOutputLevel);
            addRow("Min Output Level", MinOutputLevel);
            addRow("Source Level", SourceLevel);
            addRow("Max Source Level", MaxSourceLevel);
            addRow("Min Source Level", MinSourceLevel);

            addRow("Input Mute", InputMute);
            addRow("Output Mute", OutputMute);
            addRow("Source Mute", SourceMute);

            addRow("Source Selection", SourceSelection);
        }

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand command in GetBaseConsoleCommands())
                yield return command;

            yield return new GenericConsoleCommand<float>("SetInputLevel", "SetInputLevel <LEVEL>", f => SetInputLevel(f));
            yield return new GenericConsoleCommand<float>("SetMaxInputLevel", "SetMaxInputLevel <LEVEL>", f => SetMaxInputLevel(f));
            yield return new GenericConsoleCommand<float>("SetMinInputLevel", "SetMinInputLevel <LEVEL>", f => SetMinInputLevel(f));

            yield return new GenericConsoleCommand<float>("SetOutputLevel", "SetOutputLevel <LEVEL>", f => SetOutputLevel(f));
            yield return new GenericConsoleCommand<float>("SetMaxOutputLevel", "SetMaxOutputLevel <LEVEL>", f => SetMaxOutputLevel(f));
            yield return new GenericConsoleCommand<float>("SetMinOutputLevel", "SetMinOutputLevel <LEVEL>", f => SetMinOutputLevel(f));

            yield return new GenericConsoleCommand<float>("SetSourceLevel", "SetSourceLevel <LEVEL>", f => SetSourceLevel(f));
            yield return new GenericConsoleCommand<float>("SetMaxSourceLevel", "SetMaxSourceLevel <LEVEL>", f => SetMaxSourceLevel(f));
            yield return new GenericConsoleCommand<float>("SetMinSourceLevel", "SetMinSourceLevel <LEVEL>", f => SetMinSourceLevel(f));

            yield return new GenericConsoleCommand<bool>("InputMute", "InputMute <TRUE/FALSE>", f => SetInputMute(f));
            yield return new GenericConsoleCommand<bool>("OuputMute", "OutputMute <TRUE/FALSE>", f => SetOutputMute(f));
            yield return new GenericConsoleCommand<bool>("SourceMute", "SourceMute <TRUE/FALSE>", f => SetSourceMute(f));
        }

        /// <summary>
        /// Workaround for 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
        {
            return base.GetConsoleCommands();
        }

        
        #endregion
    }
}