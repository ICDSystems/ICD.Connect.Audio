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

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp
{
	public sealed class VoIpTransmitBlock : AbstractIoBlock
	{
		private const string LINE_COUNT_ATTRIBUTE = "numChannels";

		public event EventHandler<IntEventArgs> OnLineCountChanged; 

		private readonly Dictionary<int, VoIpTransmitLine> m_Lines;
		private readonly SafeCriticalSection m_LinesSection;

		private int m_LineCount;

		[PublicAPI]
		public int LineCount
		{
			get { return m_LineCount; }
			private set
			{
				if (value == m_LineCount)
					return;

				m_LineCount = value;

				Log(eSeverity.Debug, "LineCount set to {0}", m_LineCount);

				RebuildLines();

				OnLineCountChanged.Raise(this, new IntEventArgs(m_LineCount));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public VoIpTransmitBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Lines = new Dictionary<int, VoIpTransmitLine>();
			m_LinesSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnLineCountChanged = null;

			base.Dispose();

			DisposeLines();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			RequestAttribute(LineCountFeedback, AttributeCode.eCommand.Get, LINE_COUNT_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the lines for this VOIP Receive block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<VoIpTransmitLine> GetLines()
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
				case eChannelType.Output:
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadLine(indices[0]);

				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing channels and rebuilds from channel count.
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
		/// Gets or creates the line at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private VoIpTransmitLine LazyLoadLine(int index)
		{
			m_LinesSection.Enter();

			try
			{
				if (!m_Lines.ContainsKey(index))
					m_Lines[index] = new VoIpTransmitLine(this, index);
				return m_Lines[index];
			}
			finally
			{
				m_LinesSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing channels.
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

		private void LineCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			LineCount = innerValue.IntValue;
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
