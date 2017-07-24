using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.Aec
{
	public sealed class AecProcessingBlock : AbstractIoBlock
	{
		private const string CHANNEL_COUNT_ATTRIBUTE = "numChannels";

		/// <summary>
		/// Raised when the system informs of a channel count change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnChannelCountChanged;

		private readonly Dictionary<int, AecProcessingChannel> m_Channels;
		private readonly SafeCriticalSection m_ChannelsSection;

		private int m_ChannelCount;

		/// <summary>
		/// Gets the number of audio channels.
		/// </summary>
		[PublicAPI]
		public int ChannelCount
		{
			get { return m_ChannelCount; }
			private set
			{
				if (value == m_ChannelCount)
					return;

				m_ChannelCount = value;

				RebuildChannels();

				OnChannelCountChanged.Raise(this, new IntEventArgs(m_ChannelCount));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public AecProcessingBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, AecProcessingChannel>();
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

			DisposeChannels();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			RequestAttribute(ChannelCountFeedback, AttributeCode.eCommand.Get, CHANNEL_COUNT_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the channels for this audio input block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<AecProcessingChannel> GetChannels()
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
				case eChannelType.Input:
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadChannel(indices[0]);

				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		#endregion

		#region Private Methods

		private void ChannelCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			ChannelCount = innerValue.IntValue;
		}

		/// <summary>
		/// Disposes the existing channels and rebuilds from channel count.
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
		/// Gets the channel at the given index. Creates if it doesnt exist.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private AecProcessingChannel LazyLoadChannel(int index)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(index))
					m_Channels[index] = new AecProcessingChannel(this, index);
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
