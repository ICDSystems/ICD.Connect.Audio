using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.Aec
{
	public sealed class AecInputChannel : AbstractAttributeChild<AecInputBlock>
	{
		private const string GAIN_ATTRIBUTE = "gain";
		private const string PEAK_OCCURRING_ATTRIBUTE = "peak";
		private const string PHANTOM_POWER_ON_ATTRIBUTE = "phantomPower";

		/// <summary>
		/// Raised when the system informs of a channel gain change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnGainChanged;

		/// <summary>
		/// Raised when the system informs of a channel phantom power state change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnPhantomPowerChanged;

		/// <summary>
		/// Raised when the system informs of a channel peak occurring state change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnPeakOccurringChanged;

		private float m_Gain;
		private bool m_PeakOccurring;
		private bool m_PhantomPower;

		#region Properties

		[PublicAPI]
		public float Gain
		{
			get { return m_Gain; }
			private set
			{
				if (Math.Abs(value - m_Gain) < 0.01f)
					return;

				m_Gain = value;

				Log(eSeverity.Informational, "Gain set to {0}", m_Gain);

				OnGainChanged.Raise(this, new FloatEventArgs(m_Gain));
			}
		}

		[PublicAPI]
		public bool PeakOccurring
		{
			get { return m_PeakOccurring; }
			private set
			{
				if (value == m_PeakOccurring)
					return;

				m_PeakOccurring = value;

				Log(eSeverity.Informational, "PeakOccurring set to {0}", m_PeakOccurring);

				OnPeakOccurringChanged.Raise(this, new BoolEventArgs(m_PeakOccurring));
			}
		}

		[PublicAPI]
		public bool PhantomPower
		{
			get { return m_PhantomPower; }
			private set
			{
				if (value == m_PhantomPower)
					return;

				m_PhantomPower = value;

				Log(eSeverity.Informational, "PhantomPower set to {0}", m_PhantomPower);

				OnPhantomPowerChanged.Raise(this, new BoolEventArgs(m_PhantomPower));
			}
		}


		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "InputChannel"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public AecInputChannel(AecInputBlock parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnGainChanged = null;
			OnPhantomPowerChanged = null;
			OnPeakOccurringChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(GainFeedback, AttributeCode.eCommand.Get, GAIN_ATTRIBUTE, null, Index);
			RequestAttribute(PeakOccurringFeedback, AttributeCode.eCommand.Get, PEAK_OCCURRING_ATTRIBUTE, null, Index);
			RequestAttribute(PhantomPowerFeedback, AttributeCode.eCommand.Get, PHANTOM_POWER_ON_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			// Subscribe
			RequestAttribute(PeakOccurringFeedback, command, PEAK_OCCURRING_ATTRIBUTE, null, Index);
			RequestAttribute(PhantomPowerFeedback, command, PHANTOM_POWER_ON_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Sets the gain for the channel in dB (0 - 66 dB).
		/// </summary>
		/// <param name="db"></param>
		[PublicAPI]
		public void SetGain(int db)
		{
			RequestAttribute(GainFeedback, AttributeCode.eCommand.Set, GAIN_ATTRIBUTE, new Value(db), Index);
		}

		/// <summary>
		/// Increments the gain in 6dB increments to 66dB.
		/// </summary>
		[PublicAPI]
		public void IncrementGain()
		{
			RequestAttribute(GainFeedback, AttributeCode.eCommand.Increment, GAIN_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Decrements the gain in 6dB increments to 6dB.
		/// </summary>
		[PublicAPI]
		public void DecrementGain()
		{
			RequestAttribute(GainFeedback, AttributeCode.eCommand.Decrement, GAIN_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetPhantomPower(bool power)
		{
			RequestAttribute(PhantomPowerFeedback, AttributeCode.eCommand.Set, PHANTOM_POWER_ON_ATTRIBUTE, new Value(power), Index);
		}

		[PublicAPI]
		public void TogglePhantomPower()
		{
			RequestAttribute(PhantomPowerFeedback, AttributeCode.eCommand.Toggle, PHANTOM_POWER_ON_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void GainFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			Gain = innerValue.FloatValue;
		}

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void PeakOccurringFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			PeakOccurring = innerValue.BoolValue;
		}

		/// <summary>
		/// Called when the system sends us feedback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="value"></param>
		private void PhantomPowerFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			PhantomPower = innerValue.BoolValue;
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

			addRow("Gain", Gain);
			addRow("Peak Occurring", PeakOccurring);
			addRow("Phantom Power", PhantomPower);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("SetGain", "SetGain <DB>", i => SetGain(i));
			yield return new ConsoleCommand("IncrementGain", "Increments the gain 6 dB", () => IncrementGain());
			yield return new ConsoleCommand("DecrementGain", "Decrements the gain 6 dB", () => DecrementGain());

			yield return new GenericConsoleCommand<bool>("SetPhantomPower", "SetPhantomPower <true/false>", b => SetPhantomPower(b));
			yield return new ConsoleCommand("TogglePhantomPower", "Toggles the current phantom power state", () => TogglePhantomPower());
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
