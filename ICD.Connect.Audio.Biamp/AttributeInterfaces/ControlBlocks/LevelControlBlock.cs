using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks
{
	public sealed class LevelControlBlock : AbstractControlBlock
	{
		private const string CHANNEL_COUNT_ATTRIBUTE = "numChannels";
		private const string CHANNELS_GANGED_ATTRIBUTE = "ganged";
		private const string USE_RAMPING_ATTRIBUTE = "useRamping";

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnChannelCountChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnGangedChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnUseRampingChanged; 

		private readonly Dictionary<int, LevelControlChannel> m_Channels;
		private readonly SafeCriticalSection m_ChannelsSection;

		private int m_ChannelCount;
		private bool m_Ganged;
		private bool m_UseRamping;

		#region Properties

		[PublicAPI]
		public int ChannelCount
		{
			get { return m_ChannelCount; }
			private set
			{
				if (value == m_ChannelCount)
					return;

				m_ChannelCount = value;

				Log(eSeverity.Debug, "ChannelCount set to {0}", m_ChannelCount);

				RebuildChannels();

				OnChannelCountChanged.Raise(this, new IntEventArgs(m_ChannelCount));
			}
		}

		[PublicAPI]
		public bool Ganged
		{
			get { return m_Ganged; }
			private set
			{
				if (value == m_Ganged)
					return;

				m_Ganged = value;

				Log(eSeverity.Debug, "Ganged set to {0}", m_Ganged);

				OnGangedChanged.Raise(this, new BoolEventArgs(m_Ganged));
			}
		}

		[PublicAPI]
		public bool UseRamping
		{
			get { return m_UseRamping; }
			private set
			{
				if (value == m_UseRamping)
					return;

				m_UseRamping = value;

				Log(eSeverity.Debug, "UseRamping set to {0}", m_UseRamping);

				OnUseRampingChanged.Raise(this, new BoolEventArgs(m_UseRamping));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public LevelControlBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, LevelControlChannel>();
			m_ChannelsSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnChannelCountChanged = null;

			base.Dispose();

			DisposeLines();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			RequestAttribute(ChannelCountFeedback, AttributeCode.eCommand.Get, CHANNEL_COUNT_ATTRIBUTE, null);
			RequestAttribute(GangedFeedback, AttributeCode.eCommand.Get, CHANNELS_GANGED_ATTRIBUTE, null);
			RequestAttribute(UseRampingFeedback, AttributeCode.eCommand.Get, USE_RAMPING_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the lines for this VOIP Receive block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<LevelControlChannel> GetChannels()
		{
			return m_ChannelsSection.Execute(() => m_Channels.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the child attribute interface at the given path.
		/// </summary>
		/// <param name="channelType"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public override IAttributeInterface GetAttributeInterface(eChannelType channelType, params int[] indices)
		{
			switch (channelType)
			{
				case eChannelType.Output:
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadChannel(indices[0]);

				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates the channels to match the channel count.
		/// </summary>
		private void RebuildChannels()
		{
			m_ChannelsSection.Enter();

			try
			{
				Enumerable.Range(1, ChannelCount).ForEach(i => LazyLoadChannel(i));
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		/// <summary>
		/// Gets or creates the channel at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private LevelControlChannel LazyLoadChannel(int index)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(index))
					m_Channels[index] = new LevelControlChannel(this, index);
				return m_Channels[index];
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing channels.
		/// </summary>
		private void DisposeLines()
		{
			m_ChannelsSection.Enter();

			try
			{
				m_Channels.Values.ForEach(c => c.Dispose());
				m_Channels.Clear();
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		#endregion

		#region Subscription Callbacks

		private void ChannelCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			ChannelCount = innerValue.IntValue;
		}

		private void GangedFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Ganged = innerValue.BoolValue;
		}

		private void UseRampingFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			UseRamping = innerValue.BoolValue;
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

			addRow("Channel Count", ChannelCount);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("Channels", GetChannels(), c => (uint)c.Index);
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
