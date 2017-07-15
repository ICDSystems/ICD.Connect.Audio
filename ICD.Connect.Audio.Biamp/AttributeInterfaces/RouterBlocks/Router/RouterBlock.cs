using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.Router
{
	public sealed class RouterBlock : AbstractRouterBlock
	{
		private const string INPUT_COUNT_ATTRIBUTE = "numInputs";
		private const string OUTPUT_COUNT_ATTRIBUTE = "numOutputs";

		public event EventHandler<IntEventArgs> OnInputCountChanged;
		public event EventHandler<IntEventArgs> OnOutputCountChanged;

		private readonly Dictionary<int, RouterInput> m_Inputs;
		private readonly SafeCriticalSection m_InputsSection;

		private readonly Dictionary<int, RouterOutput> m_Outputs;
		private readonly SafeCriticalSection m_OutputsSection;

		private int m_InputCount;
		private int m_OutputCount;

		#region Properties

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
		public RouterBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
			m_Inputs = new Dictionary<int, RouterInput>();
			m_InputsSection = new SafeCriticalSection();

			m_Outputs = new Dictionary<int, RouterOutput>();
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
		/// Gets the inputs for this router block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<RouterInput> GetInputs()
		{
			return m_InputsSection.Execute(() => m_Inputs.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the outputs for this router block.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<RouterOutput> GetOutputs()
		{
			return m_OutputsSection.Execute(() => m_Outputs.OrderValuesByKey().ToArray());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Builds inputs for the input count.
		/// </summary>
		private void RebuildInputs()
		{
			m_InputsSection.Enter();

			try
			{
				Enumerable.Range(0, InputCount).ForEach(i => LazyLoadInput(i));
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Gets or creates the input for the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private RouterInput LazyLoadInput(int index)
		{
			m_InputsSection.Enter();

			try
			{
				if (!m_Inputs.ContainsKey(index))
					m_Inputs[index] = new RouterInput(this, index);
				return m_Inputs[index];
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
		/// Builds outputs for the output count.
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
		/// Gets or creates the output for the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private RouterOutput LazyLoadOutput(int index)
		{
			m_OutputsSection.Enter();

			try
			{
				if (!m_Outputs.ContainsKey(index))
					m_Outputs[index] = new RouterOutput(this, index);
				return m_Outputs[index];
			}
			finally
			{
				m_OutputsSection.Leave();
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
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				InputCount = innerValue.IntValue;
		}

		private void OutputCountFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value["value"] as Value;
			if (innerValue != null)
				OutputCount = innerValue.IntValue;
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

			yield return ConsoleNodeGroup.KeyNodeMap("Inputs", GetInputs(), s => (uint)s.Index);
			yield return ConsoleNodeGroup.KeyNodeMap("Outputs", GetOutputs(), s => (uint)s.Index);
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
