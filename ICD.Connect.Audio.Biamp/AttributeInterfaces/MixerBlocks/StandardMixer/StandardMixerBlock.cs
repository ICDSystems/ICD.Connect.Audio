using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Nodes;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.StandardMixer
{
	public sealed class StandardMixerBlock : AbstractMixerBlock
	{
		private const string INPUT_COUNT_ATTRIBUTE = "numInputs";
		private const string OUTPUT_COUNT_ATTRIBUTE = "numOutputs";
		private const string CROSSPOINT_ON_ATTRIBUTE = "crosspoint";
		private const string CROSSPOINT_COLUMN_ATTRIBUTE = "crosspointColumn";
		private const string CROSSPOINT_DIAGONAL_ATTRIBUTE = "crosspointDiagonal";
		private const string CROSSPOINT_ROW_ATTRIBUTE = "crosspointRow";

		/// <summary>
		/// Raised when the system informs of an input count change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnInputCountChanged;

		/// <summary>
		/// Raised when the system informs of an output count change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnOutputCountChanged;

		private readonly Dictionary<int, StandardMixerInput> m_Inputs;
		private readonly SafeCriticalSection m_InputsSection;

		private readonly Dictionary<int, StandardMixerOutput> m_Outputs;
		private readonly SafeCriticalSection m_OutputsSection;

		private int m_InputCount;
		private int m_OutputCount;

		#region Properties

		/// <summary>
		/// Gets the number of inputs.
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

				RebuildInputs();

				OnInputCountChanged.Raise(this, new IntEventArgs(m_InputCount));
			}
		}

		/// <summary>
		/// Gets the number of outputs.
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

				RebuildOutputs();

				OnOutputCountChanged.Raise(this, new IntEventArgs(m_OutputCount));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public StandardMixerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Inputs = new Dictionary<int, StandardMixerInput>();
			m_InputsSection = new SafeCriticalSection();

			m_Outputs = new Dictionary<int, StandardMixerOutput>();
			m_OutputsSection = new SafeCriticalSection();

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

			base.Dispose();

			DisposeInputs();
			DisposeOutputs();
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
		}

		/// <summary>
		/// Gets the inputs for this mixer block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<StandardMixerInput> GetInputs()
		{
			return m_InputsSection.Execute(() => m_Inputs.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the outputs for this mixer block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<StandardMixerOutput> GetOutputs()
		{
			return m_OutputsSection.Execute(() => m_Outputs.OrderValuesByKey().ToArray());
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
					return LazyLoadInput(indices[0]);

				case eChannelType.Output:
					if (indices.Length != 1)
						throw new ArgumentOutOfRangeException("indices");
					return LazyLoadOutput(indices[0]);

				default:
					return base.GetAttributeInterface(channelType, indices);
			}
		}

		[PublicAPI]
		public void SetCrosspointColumnOn(int output, bool on)
		{
			RequestAttribute(CrosspointColumnOnFeedback, AttributeCode.eCommand.Set, CROSSPOINT_COLUMN_ATTRIBUTE, new Value(on), output);
		}

		[PublicAPI]
		public void ToggleCrosspointColumnOn(int output)
		{
			RequestAttribute(CrosspointColumnOnFeedback, AttributeCode.eCommand.Toggle, CROSSPOINT_COLUMN_ATTRIBUTE, null, output);
		}

		[PublicAPI]
		public void SetCrosspointRowOn(int input, bool on)
		{
			RequestAttribute(CrosspointRowOnFeedback, AttributeCode.eCommand.Set, CROSSPOINT_ROW_ATTRIBUTE, new Value(on), input);
		}

		[PublicAPI]
		public void ToggleCrosspointRowOn(int input)
		{
			RequestAttribute(CrosspointRowOnFeedback, AttributeCode.eCommand.Toggle, CROSSPOINT_ROW_ATTRIBUTE, null, input);
		}

		[PublicAPI]
		public void SetCrosspointDiagonalOn(int input, int output, bool on)
		{
			RequestAttribute(CrosspointDiagonalOnFeedback, AttributeCode.eCommand.Set, CROSSPOINT_DIAGONAL_ATTRIBUTE, new Value(on), input, output);
		}

		[PublicAPI]
		public void ToggleCrosspointDiagonalOn(int input, int output)
		{
			RequestAttribute(CrosspointDiagonalOnFeedback, AttributeCode.eCommand.Toggle, CROSSPOINT_DIAGONAL_ATTRIBUTE, null, input, output);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Disposes the existing inputs and rebuilds from input count.
		/// </summary>
		private void RebuildInputs()
		{
			m_InputsSection.Enter();

			try
			{
				Enumerable.Range(1, InputCount).ForEach(i => LazyLoadInput(i));
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the input at the given index. Creates the input if it does not exist.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private StandardMixerInput LazyLoadInput(int index)
		{
			m_InputsSection.Enter();

			try
			{
				if (!m_Inputs.ContainsKey(index))
					m_Inputs[index] = new StandardMixerInput(this, index);
				return m_Inputs[index];
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing outputs and rebuilds from output count.
		/// </summary>
		private void RebuildOutputs()
		{
			m_OutputsSection.Enter();

			try
			{
				Enumerable.Range(1, OutputCount).ForEach(i => LazyLoadOutput(i));
			}
			finally
			{
				m_OutputsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the output at the given index. Creates the output if it does not exist.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private StandardMixerOutput LazyLoadOutput(int index)
		{
			m_InputsSection.Enter();

			try
			{
				if (!m_Outputs.ContainsKey(index))
					m_Outputs[index] = new StandardMixerOutput(this, index);
				return m_Outputs[index];
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing inputs.
		/// </summary>
		private void DisposeInputs()
		{
			m_InputsSection.Enter();

			try
			{
				m_Inputs.Values.ForEach(c => c.Dispose());
				m_Inputs.Clear();
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes the existing outputs.
		/// </summary>
		private void DisposeOutputs()
		{
			m_OutputsSection.Enter();

			try
			{
				m_Outputs.Values.ForEach(c => c.Dispose());
				m_Outputs.Clear();
			}
			finally
			{
				m_OutputsSection.Leave();
			}
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

		private void CrosspointColumnOnFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void CrosspointRowOnFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			// todo
		}

		private void CrosspointDiagonalOnFeedback(BiampTesiraDevice sender, ControlValue value)
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

			addRow("Input Count", InputCount);
			addRow("Output Count", OutputCount);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.KeyNodeMap("Inputs", GetInputs(), c => (uint)c.Index);
			yield return ConsoleNodeGroup.KeyNodeMap("Outputs", GetOutputs(), c => (uint)c.Index);
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
