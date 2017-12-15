using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp
{
	public sealed class VoIpControlStatusBlock : AbstractIoBlock
	{
		private const string CALL_STATE_ATTRIBUTE = "callState";
		//private const string STATISTICS_ATTRIBUTE = "cardStat";
		private const string NAT_INFO_ATTRIBUTE = "nat";
		private const string NETWORK_INFO_ATTRIBUTE = "network";
		private const string LINE_COUNT_ATTRIBUTE = "numChannels";
		private const string PROTOCOL_INFO_ATTRIBUTE = "protocols";
		private const string SYNCHRONIZED_TIME_ATTRIBUTE = "syncTime";

		private readonly Dictionary<int, VoIpControlStatusLine> m_Lines;
		private readonly SafeCriticalSection m_LinesSection;

		#region Properties

		[PublicAPI]
		public int LineCount
		{
			get { return 2; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public VoIpControlStatusBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Lines = new Dictionary<int, VoIpControlStatusLine>();
			m_LinesSection = new SafeCriticalSection();

			RebuildLines();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			DisposeLines();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(CallStateFeedback, AttributeCode.eCommand.Get, CALL_STATE_ATTRIBUTE, null);
			//RequestAttribute(StatisticsFeedback, AttributeCode.eCommand.Get, STATISTICS_ATTRIBUTE, null);
			RequestAttribute(NatInfoFeedback, AttributeCode.eCommand.Get, NAT_INFO_ATTRIBUTE, null);
			RequestAttribute(NetworkInfoFeedback, AttributeCode.eCommand.Get, NETWORK_INFO_ATTRIBUTE, null);
			RequestAttribute(ProtocolInfoFeedback, AttributeCode.eCommand.Get, PROTOCOL_INFO_ATTRIBUTE, null);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			// Subscribe
			RequestAttribute(CallStateFeedback, command, CALL_STATE_ATTRIBUTE, null);
			//RequestAttribute(StatisticsFeedback, command, STATISTICS_ATTRIBUTE, null);
			RequestAttribute(NatInfoFeedback, command, NAT_INFO_ATTRIBUTE, null);
			RequestAttribute(NetworkInfoFeedback, command, NETWORK_INFO_ATTRIBUTE, null);
			RequestAttribute(ProtocolInfoFeedback, command, PROTOCOL_INFO_ATTRIBUTE, null);
		}

		[PublicAPI]
		public IEnumerable<VoIpControlStatusLine> GetLines()
		{
			return m_LinesSection.Execute(() => m_Lines.OrderValuesByKey().ToArray());
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
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadLine(indices[0]);

				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		[PublicAPI]
		public void SetSynchronizedTime(DateTime time)
		{
			RequestAttribute(SynchronizedTimeFeedback, AttributeCode.eCommand.Set, SYNCHRONIZED_TIME_ATTRIBUTE, new Value(time));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing lines and rebuilds from line count.
		/// </summary>
		private void RebuildLines()
		{
			m_LinesSection.Enter();

			try
			{
				Enumerable.Range(1, LineCount).ForEach(i => LazyLoadLine(i));
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		/// <summary>
		/// Gets the channel at the given index. If the channel doesn't exist, creates a new one.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private VoIpControlStatusLine LazyLoadLine(int index)
		{
			m_LinesSection.Enter();

			try
			{
				if (!m_Lines.ContainsKey(index))
					m_Lines[index] = new VoIpControlStatusLine(this, index);
				return m_Lines[index];
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing lines.
		/// </summary>
		private void DisposeLines()
		{
			m_LinesSection.Enter();

			try
			{
				m_Lines.Values.ForEach(c => c.Dispose());
				m_Lines.Clear();
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		#endregion

		#region Subscription Callbacks

		private void CallStateFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ControlValue result = value.GetValue<ControlValue>("value");
			ArrayValue callStates = result.GetValue<ArrayValue>("callStateInfo");

			foreach (ControlValue callState in callStates.Cast<ControlValue>())
			{
				Value lineIdValue = callState.GetValue<Value>("lineId");

				int lineId = lineIdValue.IntValue;
				LazyLoadLine(lineId + 1).ParseCallState(callState);
			}
		}

		private void StatisticsFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void NatInfoFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void NetworkInfoFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void ProtocolInfoFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void SynchronizedTimeFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
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

			addRow("Line Count", LineCount);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("Lines", GetLines(), c => (uint)c.Index);
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
