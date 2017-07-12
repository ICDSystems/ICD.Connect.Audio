using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.SourceSelector
{
	public sealed class SourceSelectorBlock : AbstractRouterBlock
	{
		private const string INPUT_COUNT_ATTRIBUTE = "numInputs";
		private const string OUTPUT_COUNT_ATTRIBUTE = "numOutputs";
		private const string SOURCE_COUNT_ATTRIBUTE = "numSources";
		private const string OUTPUT_LEVEL_ATTRIBUTE = "outputLevel";
		private const string MIN_OUTPUT_LEVEL_ATTRIBUTE = "outputMinLevel";
		private const string MAX_OUTPUT_LEVEL_ATTRIBUTE = "outputMaxLevel";
		private const string OUTPUT_MUTE_ATTRIBUTE = "outputMute";
		private const string SOURCE_SELECTION_ATTRIBUTE = "sourceSelection";
		private const string STEREO_ENABLED_ATTRIBUTE = "stereoEnable";

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnInputCountChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnOutputCountChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnSourceCountChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnSourceSelectionChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnOutputLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMinOutputLevelChanged;

		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnMaxOutputLevelChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnOutputMuteChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnStereoEnabledChanged;

		private readonly Dictionary<int, SourceSelectorSource> m_Sources;
		private readonly SafeCriticalSection m_SourcesSection;

		private int m_InputCount;
		private int m_OutputCount;
		private int m_SourceCount;

		private int m_SourceSelection;

		private float m_OutputLevel;
		private float m_MinOutputLevel;
		private float m_MaxOutputLevel;

		private bool m_OutputMute;
		private bool m_StereoEnabled;

		#region Properties

		/// <summary>
		/// Gets the cached input count.
		/// </summary>
		[PublicAPI]
		public int InputCount
		{
			get { return m_InputCount; }
			private set
			{
				if (value == m_InputCount)
					return;

				m_InputCount = value;

				OnInputCountChanged.Raise(this, new IntEventArgs(m_InputCount));
			}
		}

		/// <summary>
		/// Gets the cached output count.
		/// </summary>
		[PublicAPI]
		public int OutputCount
		{
			get { return m_OutputCount; }
			private set
			{
				if (value == m_OutputCount)
					return;

				m_OutputCount = value;

				OnOutputCountChanged.Raise(this, new IntEventArgs(m_OutputCount));
			}
		}

		/// <summary>
		/// Gets the cached source count.
		/// </summary>
		[PublicAPI]
		public int SourceCount
		{
			get { return m_SourceCount; }
			private set
			{
				if (value == m_SourceCount)
					return;

				m_SourceCount = value;

				RebuildSources();

				OnSourceCountChanged.Raise(this, new IntEventArgs(m_SourceCount));
			}
		}

		/// <summary>
		/// Gets the cached source selection.
		/// </summary>
		[PublicAPI]
		public int SourceSelection
		{
			get { return m_SourceSelection; }
			private set
			{
				if (value == m_SourceSelection)
					return;

				m_SourceSelection = value;

				OnSourceSelectionChanged.Raise(this, new IntEventArgs(m_SourceSelection));
			}
		}

		/// <summary>
		/// Gets the cached output level.
		/// </summary>
		[PublicAPI]
		public float OutputLevel
		{
			get { return m_OutputLevel; }
			private set
			{
				if (value == m_OutputLevel)
					return;

				m_OutputLevel = value;

				OnOutputLevelChanged.Raise(this, new FloatEventArgs(m_OutputLevel));
			}
		}

		/// <summary>
		/// Gets the cached min output level.
		/// </summary>
		[PublicAPI]
		public float MinOutputLevel
		{
			get { return m_MinOutputLevel; }
			private set
			{
				if (value == m_MinOutputLevel)
					return;

				m_MinOutputLevel = value;

				OnMinOutputLevelChanged.Raise(this, new FloatEventArgs(m_MinOutputLevel));
			}
		}

		/// <summary>
		/// Gets the cached max output level.
		/// </summary>
		[PublicAPI]
		public float MaxOutputLevel
		{
			get { return m_MaxOutputLevel; }
			private set
			{
				if (value == m_MaxOutputLevel)
					return;

				m_MaxOutputLevel = value;

				OnMaxOutputLevelChanged.Raise(this, new FloatEventArgs(m_MaxOutputLevel));
			}
		}

		/// <summary>
		/// Gets the cached output mute state.
		/// </summary>
		[PublicAPI]
		public bool OutputMute
		{
			get { return m_OutputMute; }
			private set
			{
				if (value == m_OutputMute)
					return;

				m_OutputMute = value;

				OnOutputMuteChanged.Raise(this, new BoolEventArgs(m_OutputMute));
			}
		}

		/// <summary>
		/// Gets the cached stereo enabled state.
		/// </summary>
		[PublicAPI]
		public bool StereoEnabled
		{
			get { return m_StereoEnabled; }
			private set
			{
				if (value == m_StereoEnabled)
					return;

				m_StereoEnabled = value;

				OnStereoEnabledChanged.Raise(this, new BoolEventArgs(m_StereoEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public SourceSelectorBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Sources = new Dictionary<int, SourceSelectorSource>();
			m_SourcesSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnInputCountChanged = null;
			OnOutputCountChanged = null;
			OnSourceCountChanged = null;
			OnSourceSelectionChanged = null;
			OnOutputLevelChanged = null;
			OnMinOutputLevelChanged = null;
			OnMaxOutputLevelChanged = null;
			OnOutputMuteChanged = null;
			OnStereoEnabledChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Unsubscribe, OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Unsubscribe, SOURCE_SELECTION_ATTRIBUTE, null);

			DisposeSources();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(InputCountFeedback, AttributeCode.eCommand.Get, INPUT_COUNT_ATTRIBUTE, null);
			RequestAttribute(OutputCountFeedback, AttributeCode.eCommand.Get, OUTPUT_COUNT_ATTRIBUTE, null);
			RequestAttribute(SourceCountFeedback, AttributeCode.eCommand.Get, SOURCE_COUNT_ATTRIBUTE, null);
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Get, OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Get, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Get, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(OutputMuteFeedback, AttributeCode.eCommand.Get, OUTPUT_MUTE_ATTRIBUTE, null);
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Get, SOURCE_SELECTION_ATTRIBUTE, null);
			RequestAttribute(StereoEnabledFeedback, AttributeCode.eCommand.Get, STEREO_ENABLED_ATTRIBUTE, null);

			// Subscribe
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Subscribe, OUTPUT_LEVEL_ATTRIBUTE, null);
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Subscribe, SOURCE_SELECTION_ATTRIBUTE, null);
		}

		/// <summary>
		/// Gets the sources for this source selector block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<SourceSelectorSource> GetSources()
		{
			return m_SourcesSection.Execute(() => m_Sources.OrderValuesByKey().ToArray());
		}

		[PublicAPI]
		public void SetOutputLevel(float level)
		{
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Set, OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementOutputLevel()
		{
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Increment, OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementOutputLevel()
		{
			RequestAttribute(OutputLevelFeedback, AttributeCode.eCommand.Decrement, OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMinOutputLevel(float level)
		{
			RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Set, MIN_OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMinOutputLevel()
		{
			RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Increment, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMinOutputLevel()
		{
			RequestAttribute(MinOutputLevelFeedback, AttributeCode.eCommand.Decrement, MIN_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetMaxOutputLevel(float level)
		{
			RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Set, MAX_OUTPUT_LEVEL_ATTRIBUTE, new Value(level));
		}

		[PublicAPI]
		public void IncrementMaxOutputLevel()
		{
			RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Increment, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementMaxOutputLevel()
		{
			RequestAttribute(MaxOutputLevelFeedback, AttributeCode.eCommand.Decrement, MAX_OUTPUT_LEVEL_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetOutputMute(bool mute)
		{
			RequestAttribute(OutputMuteFeedback, AttributeCode.eCommand.Set, OUTPUT_MUTE_ATTRIBUTE, new Value(mute));
		}

		[PublicAPI]
		public void ToggleOutputMute()
		{
			RequestAttribute(OutputMuteFeedback, AttributeCode.eCommand.Toggle, OUTPUT_MUTE_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetSourceSelection(int source)
		{
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Set, SOURCE_SELECTION_ATTRIBUTE, new Value(source));
		}

		[PublicAPI]
		public void ClearSourceSelection()
		{
			SetSourceSelection(0);
		}

		[PublicAPI]
		public void IncrementSourceSelection()
		{
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Increment, SOURCE_SELECTION_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void DecrementSourceSelection()
		{
			RequestAttribute(SourceSelectionFeedback, AttributeCode.eCommand.Decrement, SOURCE_SELECTION_ATTRIBUTE, null);
		}

		#endregion

		#region Subscription Callbacks

		private void InputCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			InputCount = (value["value"] as Value).IntValue;
		}

		private void OutputCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			OutputCount = (value["value"] as Value).IntValue;
		}

		private void SourceCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			SourceCount = (value["value"] as Value).IntValue;
		}

		private void MinOutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MinOutputLevel = (value["value"] as Value).FloatValue;
		}

		private void MaxOutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			MaxOutputLevel = (value["value"] as Value).FloatValue;
		}

		private void OutputMuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			OutputMute = (value["value"] as Value).BoolValue;
		}

		private void StereoEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			StereoEnabled = (value["value"] as Value).BoolValue;
		}

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void OutputLevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			OutputLevel = (value["value"] as Value).FloatValue;
		}

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void SourceSelectionFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			SourceSelection = (value["value"] as Value).IntValue;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing sources and rebuilds from source count.
		/// </summary>
		private void RebuildSources()
		{
			m_SourcesSection.Enter();

			try
			{
				Enumerable.Range(1, SourceCount).ForEach(i => LazyLoadSource(i));
			}
			finally
			{
				m_SourcesSection.Leave();
			}
		}

		/// <summary>
		/// Gets or creates the source for the given channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		private SourceSelectorSource LazyLoadSource(int channel)
		{
			m_SourcesSection.Enter();

			try
			{
				if (!m_Sources.ContainsKey(channel))
					m_Sources[channel] = new SourceSelectorSource(this, channel);
				return m_Sources[channel];
			}
			finally
			{
				m_SourcesSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing sources.
		/// </summary>
		private void DisposeSources()
		{
			m_SourcesSection.Enter();

			try
			{
				m_Sources.Values.ForEach(c => c.Dispose());
				m_Sources.Clear();
			}
			finally
			{
				m_SourcesSection.Leave();
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

			addRow("Input Count", InputCount);
			addRow("Output Count", OutputCount);
			addRow("Source Count", SourceCount);
			addRow("Source Selection", SourceSelection);
			addRow("Output Level", OutputLevel);
			addRow("Min Output Level", MinOutputLevel);
			addRow("Max Output Level", MaxOutputLevel);
			addRow("Output Mute", OutputMute);
			addRow("Stereo Enabled", StereoEnabled);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<float>("SetOutputLevel", "SetOutputLevel <LEVEL>", f => SetOutputLevel(f));
			yield return new ConsoleCommand("IncrementOutputLevel", "Increments the output level", () => IncrementOutputLevel());
			yield return new ConsoleCommand("DecrementOutputLevel", "Decrements the output level", () => DecrementOutputLevel());

			yield return new GenericConsoleCommand<float>("SetMinOutputLevel", "SetMinOutputLevel <LEVEL>", f => SetMinOutputLevel(f));
			yield return new ConsoleCommand("IncrementMinOutputLevel", "Increments the min output level", () => IncrementMinOutputLevel());
			yield return new ConsoleCommand("DecrementMinOutputLevel", "Decrements the min output level", () => DecrementMinOutputLevel());

			yield return new GenericConsoleCommand<float>("SetMaxOutputLevel", "SetMaxOutputLevel <LEVEL>", f => SetMaxOutputLevel(f));
			yield return new ConsoleCommand("IncrementMaxOutputLevel", "Increments the max output level", () => IncrementMaxOutputLevel());
			yield return new ConsoleCommand("DecrementMaxOutputLevel", "Decrements the max output level", () => DecrementMaxOutputLevel());

			yield return new GenericConsoleCommand<bool>("SetOutputMute", "SetOutputMute <true/false>", b => SetOutputMute(b));
			yield return new ConsoleCommand("ToggleOutputMute", "Toggles the current output mute state", () => ToggleOutputMute());

			yield return new GenericConsoleCommand<int>("SetSourceSelection", "SetSourceSelection <SOURCE>", i => SetSourceSelection(i));
			yield return new ConsoleCommand("ClearSourceSelection", "Clears the current source selection", () => ClearSourceSelection());
			yield return new ConsoleCommand("IncrementSourceSelection", "Increments the current source selection", () => IncrementSourceSelection());
			yield return new ConsoleCommand("DecrementSourceSelection", "Decrements the current source selection", () => DecrementSourceSelection());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("Sources", GetSources(), s => (uint)s.Index);
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
