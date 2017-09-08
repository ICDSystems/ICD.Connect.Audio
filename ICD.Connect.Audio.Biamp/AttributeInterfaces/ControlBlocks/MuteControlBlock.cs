using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
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
	public sealed class MuteControlBlock : AbstractControlBlock
	{
		private const string CHANNEL_COUNT_ATTRIBUTE = "numChannels";
		private const string CHANNELS_GANGED_ATTRIBUTE = "ganged";

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnChannelCountChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnGangedChanged;

		private readonly Dictionary<int, MuteControlChannel> m_Channels;
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

				Log(eSeverity.Informational, "ChannelCount set to {0}", m_ChannelCount);

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

				Log(eSeverity.Informational, "Ganged set to {0}", m_Ganged);

				OnGangedChanged.Raise(this, new BoolEventArgs(m_Ganged));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public MuteControlBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, MuteControlChannel>();
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
			OnGangedChanged = null;

			base.Dispose();

			DisposeChannels();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			RequestAttribute(ChannelCountFeedback, AttributeCode.eCommand.Get, CHANNEL_COUNT_ATTRIBUTE, null);
			RequestAttribute(GangedFeedback, AttributeCode.eCommand.Get, CHANNELS_GANGED_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the lines for this VOIP Receive block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<MuteControlChannel> GetChannels()
		{
			return m_ChannelsSection.Execute(() => m_Channels.OrderValuesByKey().ToArray());
		}

		[PublicAPI]
		public MuteControlChannel GetChannel(int channel)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(channel))
					m_Channels[channel] = new MuteControlChannel(this, channel);
				return m_Channels[channel];
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
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
				case eChannelType.None:
					return GetChannel(indices[0]);
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
		private MuteControlChannel LazyLoadChannel(int index)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(index))
					m_Channels[index] = new MuteControlChannel(this, index);
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
		private void DisposeChannels()
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
