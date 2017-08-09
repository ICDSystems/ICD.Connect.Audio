using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
    public sealed class RoomCombinerWall : AbstractAttributeChild<RoomCombinerBlock>
    {
        private const string WALL_STATE_ATTRIBUTE = "wallState";

        public event EventHandler<BoolEventArgs> OnWallClosedChanged;

        private bool m_WallState;

        [PublicAPI]
        public bool WallClosed
        {
            get { return m_WallState; }
            private set
            {
                if (value == m_WallState)
                    return;
                m_WallState = value;
                Log(eSeverity.Informational, "Wall Closed set to {0}", m_WallState);
                OnWallClosedChanged.Raise(this, new BoolEventArgs(m_WallState));
            }
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

            //Subscribe
            RequestAttribute(WallStateFeedback, AttributeCode.eCommand.Get, WALL_STATE_ATTRIBUTE, null, Index);

        }

        private void WallStateFeedback(BiampTesiraDevice sender, ControlValue value)
        {
            Value innerValue = value.GetValue<Value>("value");
            WallClosed = innerValue.BoolValue;
        }

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
    }
}