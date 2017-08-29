using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MeterBlocks
{
	public sealed class PeakOrRmsMeterBlock : AbstractMeterBlock
	{
		private const string CHANNEL_COUNT_ATTRIBUTE = "numChannels";

		public event EventHandler<IntEventArgs> OnChannelCountChanged;

		private readonly Dictionary<int, PeakOrRmsMeterChannel> m_Channels;
		private readonly SafeCriticalSection m_ChannelsSection;

		private int m_ChannelCount;

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

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public PeakOrRmsMeterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, PeakOrRmsMeterChannel>();
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

			// Get initial values
			RequestAttribute(ChannelCountFeedback, AttributeCode.eCommand.Get, CHANNEL_COUNT_ATTRIBUTE, null);
		}

		#endregion

		#region Private Methods

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

		private PeakOrRmsMeterChannel LazyLoadChannel(int index)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(index))
					m_Channels[index] = new PeakOrRmsMeterChannel(this, index);
				return m_Channels[index];
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

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

		#region Subscription Feedback

		private void ChannelCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			ChannelCount = innerValue.IntValue;
		}

		#endregion
	}
}
