using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.API.Nodes;
using System.Collections.Generic;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner
{
    public sealed class RoomCombinerWall : AbstractAttributeChild<RoomCombinerBlock>
    {
        #region Properties
        private const string WALL_STATE_ATTRIBUTE = "wallState";
        private const string WALL_ROOM_PRECEDENCE = "preferredRoom";

        [PublicAPI]
        public event EventHandler<BoolEventArgs> OnWallClosedChanged;
        [PublicAPI]
        public event EventHandler<IntEventArgs> OnRoomPrecedenceChanged;


        private bool m_WallClosed;
        private int m_RoomPrecedence;

        [PublicAPI]
        public bool WallClosed
        {
            get { return m_WallClosed; }
            private set
            {
                if (value == m_WallClosed)
                    return;
                m_WallClosed = value;
                Log(eSeverity.Informational, "Wall Closed set to {0}", m_WallClosed);
                OnWallClosedChanged.Raise(this, new BoolEventArgs(m_WallClosed));
            }
        }

        [PublicAPI]
        public int RoomPrecedence
        {
            get { return m_RoomPrecedence; }
            private set
            {
                if (value == m_RoomPrecedence)
                    return;

                m_RoomPrecedence = value;
                Log(eSeverity.Informational, "Room Precedence set to {0}", m_RoomPrecedence);
                OnRoomPrecedenceChanged.Raise(this, new IntEventArgs(m_RoomPrecedence));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        public RoomCombinerWall(RoomCombinerBlock parent, int index)
            : base(parent, index)
        {
            if (Device.Initialized)
                Initialize();
        }

        /// <summary>
        /// Gets the name of the index, used with logging.
        /// </summary>
        protected override string IndexName { get { return "Wall"; } }

        public override void Deinitialize()
        {
            base.Deinitialize();
        }

        /// <summary>
        /// Override to request initial values from the device, and subscribe for feedback.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            RequestAttribute(WallStateFeedback, AttributeCode.eCommand.Get, WALL_STATE_ATTRIBUTE, null, Index);
            RequestAttribute(RoomPrecedenceFeedback, AttributeCode.eCommand.Get, WALL_ROOM_PRECEDENCE, null, Index);
        }

        [PublicAPI]
        public void SetWallClosed(bool value)
        {
            RequestAttribute(WallStateFeedback, AttributeCode.eCommand.Set, WALL_STATE_ATTRIBUTE, new Value(value), Index);
        }

        [PublicAPI]
        public void SetRoomPrecedence(int precedence)
        {
            RequestAttribute(RoomPrecedenceFeedback, AttributeCode.eCommand.Set, WALL_ROOM_PRECEDENCE, new Value(precedence), Index);
        }

        #endregion

        #region Private Methods

        private void WallStateFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            WallClosed = innerValue.BoolValue;
        }

        private void RoomPrecedenceFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            RoomPrecedence = innerValue.IntValue;
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

            addRow("Wall Closed", WallClosed);
            addRow("Room Precedence", RoomPrecedence);
        }

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand command in GetBaseConsoleCommands())
                yield return command;

            yield return new GenericConsoleCommand<bool>("SetWallClosed", "SetWallClosed <TRUE/FALSE>", f => SetWallClosed(f));
            yield return new GenericConsoleCommand<int>("SetRoomPrecedence", "SetRoomPrecedence <INDEX>", f => SetRoomPrecedence(f));
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