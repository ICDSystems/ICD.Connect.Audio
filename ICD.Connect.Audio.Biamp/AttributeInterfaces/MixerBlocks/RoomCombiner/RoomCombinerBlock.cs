using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.API.Nodes;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner
{
	public sealed class RoomCombinerBlock : AbstractMixerBlock
    {
        #region Properties

        private const string LAST_MIC_HOLD_ENABLED_ATTRIBUTE = "lastMicHoldEnable";
	    private const string OPEN_MIC_LIMIT_ATTRIBUTE = "nomLimit";
	    private const string OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE = "nomLimitEnable";

	    private readonly Dictionary<int, RoomCombinerWall> m_Walls;
        private readonly Dictionary<int, RoomCombinerRoom> m_Rooms;
	    private readonly Dictionary<int, RoomCombinerSource> m_Sources;

	    private readonly SafeCriticalSection m_WallsSection;
	    private readonly SafeCriticalSection m_RoomsSection;
	    private readonly SafeCriticalSection m_SourcesSection;

        [PublicAPI]
	    public event EventHandler<BoolEventArgs> OnLastMicHoldEnabledChanged;
        [PublicAPI]
	    public event EventHandler<IntEventArgs> OnOpenMicLimitChanged;
        [PublicAPI]
	    public event EventHandler<BoolEventArgs> OnOpenMicLimitEnabledChanged;

	    private bool m_LastMicHoldEnabled;
	    private int m_OpenMicLimit;
	    private bool m_OpenMicLimitEnabled;

	    [PublicAPI]
	    public bool LastMicHoldEnabled
	    {
	        get { return m_LastMicHoldEnabled; }
	        private set
	        {
	            if (value == m_LastMicHoldEnabled)
	                return;
	           m_LastMicHoldEnabled = value;
                Log(eSeverity.Informational, "Last Mic Hold Enabled set to {0}", m_LastMicHoldEnabled);
                OnLastMicHoldEnabledChanged.Raise(this, new BoolEventArgs(m_LastMicHoldEnabled));
	        }
	    }

	    [PublicAPI]
	    public int OpenMicLimit
	    {
	        get { return m_OpenMicLimit; }
	        private set
	        {
	            if (value == m_OpenMicLimit)
	                return;
	            m_OpenMicLimit = value;
                Log(eSeverity.Informational, "Open Mic Limit set to {0}", m_OpenMicLimit);
                OnOpenMicLimitChanged.Raise(this, new IntEventArgs(m_OpenMicLimit));
	        }
	    }

	    [PublicAPI]
	    public bool OpenMicLimitEnabled
	    {
	        get { return m_OpenMicLimitEnabled; }
	        private set
	        {
	            if (value == m_OpenMicLimitEnabled)
	                return;
	            m_OpenMicLimitEnabled = value;
                Log(eSeverity.Informational, "Open Mic Limit Enabled set to {0}", m_OpenMicLimitEnabled);
                OnOpenMicLimitEnabledChanged.Raise(this, new BoolEventArgs(m_OpenMicLimitEnabled));
	        }
        }

        #endregion
        #region Public Methods

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public RoomCombinerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
            m_Walls = new Dictionary<int, RoomCombinerWall>();
            m_WallsSection = new SafeCriticalSection();

            m_Rooms = new Dictionary<int, RoomCombinerRoom>();
            m_RoomsSection = new SafeCriticalSection();

		    m_Sources = new Dictionary<int, RoomCombinerSource>();
            m_SourcesSection = new SafeCriticalSection();
        }



        public override void Initialize()
	    {
	        base.Initialize();

            RequestAttribute(LastMicHoldEnabledFeedback, AttributeCode.eCommand.Get, LAST_MIC_HOLD_ENABLED_ATTRIBUTE, null);
            RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Get, OPEN_MIC_LIMIT_ATTRIBUTE, null);
            RequestAttribute(OpenMicLimitEnabledFeedback, AttributeCode.eCommand.Get, OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE, null);
	    }

	    [PublicAPI]
	    public void SetLastMicHoldEnabled(bool value)
	    {
	        RequestAttribute(LastMicHoldEnabledFeedback, AttributeCode.eCommand.Set, LAST_MIC_HOLD_ENABLED_ATTRIBUTE, new Value(value));
	    }

	    [PublicAPI]
	    public void SetOpenMicLimit(int limit)
	    {
	        RequestAttribute(OpenMicLimitFeedback, AttributeCode.eCommand.Set, OPEN_MIC_LIMIT_ATTRIBUTE, new Value(limit));
	    }

	    [PublicAPI]
	    public void SetOpenMicLimitEnabled(bool value)
	    {
	        RequestAttribute(OpenMicLimitEnabledFeedback, AttributeCode.eCommand.Set, OPEN_MIC_LIMIT_ENABLED_ATTRIBUTE, new Value(value));
	    }

        /// <summary>
        /// Lazy-loads the wall with the given id.
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        public RoomCombinerWall GetWall(int id)
        {
            m_WallsSection.Enter();
            try
            {
                if (!m_Walls.ContainsKey(id))
                    m_Walls.Add(id, new RoomCombinerWall(this, id));
                return m_Walls[id];
            }
            finally
            {
                m_WallsSection.Leave();
            }
        }

	    [PublicAPI]
	    public IEnumerable<RoomCombinerWall> GetWalls()
	    {
	        m_WallsSection.Enter();
	        try
	        {
	            return m_Walls.Values;
	        }
	        finally
	        {
	            m_WallsSection.Leave();
	        }
	    }
        
        [PublicAPI]
        public RoomCombinerRoom GetRoom(int id)
        {
            m_RoomsSection.Enter();
            try
            {
                if (!m_Rooms.ContainsKey(id))
                    m_Rooms.Add(id, new RoomCombinerRoom(this, id));
                return m_Rooms[id];
            }
            finally
            {
                m_RoomsSection.Leave();
            }
        }

	    [PublicAPI]
	    public IEnumerable<RoomCombinerRoom> GetRooms()
	    {
	        m_RoomsSection.Enter();
	        try
	        {
	            return m_Rooms.Values;
	        }
	        finally
	        {
	            m_RoomsSection.Leave();
	        }
	    }
        [PublicAPI]
	    public RoomCombinerSource GetSource(int id)
	    {
	        m_SourcesSection.Enter();
	        try
	        {
                if (!m_Sources.ContainsKey(id))
                    m_Sources.Add(id, new RoomCombinerSource(this, id));
	            return m_Sources[id];
	        }
	        finally
	        {
	            m_SourcesSection.Leave();
	        }
        }

	    [PublicAPI]
	    public IEnumerable<RoomCombinerSource> GetSources()
	    {
	        m_SourcesSection.Enter();
	        try
	        {
	            return m_Sources.Values;
	        }
	        finally
	        {
	            m_SourcesSection.Leave();
	        }
	    }

        #endregion

        #region Private Properties

        private void LastMicHoldEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
	    {
	        Value lastMicHoldFeedbackValue = value.GetValue<Value>("value");
	        LastMicHoldEnabled = lastMicHoldFeedbackValue.BoolValue;
	    }

	    private void OpenMicLimitFeedback(BiampTesiraDevice sender, ControlValue value)
	    {
	        Value openMicLimitValue = value.GetValue<Value>("value");
	        OpenMicLimit = openMicLimitValue.IntValue;
	    }

	    private void OpenMicLimitEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
	    {
	        Value openMicLimitEnabledValue = value.GetValue<Value>("value");
	        OpenMicLimitEnabled = openMicLimitEnabledValue.BoolValue;
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

            addRow("Last Mic Hold Enabled", LastMicHoldEnabled);
            addRow("Open Mic Limit", OpenMicLimit);
            addRow("Open Mic Limit Enabled", OpenMicLimitEnabled);
        }

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand command in GetBaseConsoleCommands())
                yield return command;

            yield return new GenericConsoleCommand<bool>("SetLastMicHoldEnabled", "SetLastMicHoldEnabled", f => SetLastMicHoldEnabled(f));
            yield return new GenericConsoleCommand<int>("SetOpenMicLimit", "SetOpenMicLimit <INDEX>", f => SetOpenMicLimit(f));
            yield return new GenericConsoleCommand<bool>("SetOpenMicLimitEnabled", "SetOpenMicLimitEnabled <TRUE/FALSE>", f => SetOpenMicLimitEnabled(f));

            yield return new GenericConsoleCommand<int>("CreateRoom", "CreateRoom <INDEX>", f => GetRoom(f));
            yield return new GenericConsoleCommand<int>("CreateWall", "CreateWall <INDEX>", f => GetWall(f));
            yield return new GenericConsoleCommand<int>("CreateSource", "CreateSource <INDEX>", f => GetSource(f));

        }

        /// <summary>
        /// Gets the child console nodes.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
        {
            foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
                yield return node;

            yield return ConsoleNodeGroup.KeyNodeMap("Walls", GetWalls(), wall => (uint)wall.Index);
            yield return ConsoleNodeGroup.KeyNodeMap("Rooms", GetRooms(), room => (uint)room.Index);
            yield return ConsoleNodeGroup.KeyNodeMap("Sources", GetSources(), source => (uint)source.Index);
        }

        private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
        {
            return base.GetConsoleNodes();
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
