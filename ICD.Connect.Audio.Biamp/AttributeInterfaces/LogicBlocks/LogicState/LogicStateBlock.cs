using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks.LogicState
{
	public sealed class LogicStateBlock : AbstractLogicBlock
	{
		private readonly Dictionary<int, LogicStateChannel> m_Channels;
		private readonly SafeCriticalSection m_ChannelsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public LogicStateBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Channels = new Dictionary<int, LogicStateChannel>();
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
			base.Dispose();

			DisposeChannels();
		}

		[PublicAPI]
		public LogicStateChannel GetChannel(int channel)
		{
			m_ChannelsSection.Enter();

			try
			{
				if (!m_Channels.ContainsKey(channel))
					m_Channels[channel] = new LogicStateChannel(this, channel);
				return m_Channels[channel];
			}
			finally
			{
				m_ChannelsSection.Leave();
			}
		}

		#endregion

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

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			LogicStateChannel[] channels = m_ChannelsSection.Execute(() => m_Channels.Values.ToArray());
			yield return ConsoleNodeGroup.KeyNodeMap("Channels", channels, c => (uint)c.Index);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("InstantiateChannel", "InstantiateChannel <CHANNEL>", c => GetChannel(c));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
