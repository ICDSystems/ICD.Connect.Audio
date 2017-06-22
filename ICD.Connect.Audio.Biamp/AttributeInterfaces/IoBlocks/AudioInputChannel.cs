using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks
{
	public sealed class AudioInputChannel : AbstractAttributeChild<AudioInputBlock>, IVolumeAttributeInterface
	{
		private const string GAIN_ATTRIBUTE = "gain";
		private const string INVERT_ATTRIBUTE = "invert";
		private const string LEVEL_ATTRIBUTE = "level";
		private const string MUTE_ATTRIBUTE = "mute";
		private const string PEAK_OCCURRING_ATTRIBUTE = "peak";
		private const string PHANTOM_POWER_ON_ATTRIBUTE = "phantomPower";

		/// <summary>
		/// Raised when the system informs of a channel gain change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnGainChanged;

		/// <summary>
		/// Raised when the system informs of a channel level change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<FloatEventArgs> OnLevelChanged;

		/// <summary>
		/// Raised when the system informs of a channel invert change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnInvertChanged;

		/// <summary>
		/// Raised when the system informs of a channel mute change.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnMuteChanged;

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
		private bool m_Invert;
		private float m_Level;
		private bool m_Mute;
		private bool m_PeakOccurring;
		private bool m_PhantomPower;

		#region Properties

		[PublicAPI]
		public float Gain
		{
			get
			{
				return m_Gain;
			}
			private set
			{
				if (value == m_Gain)
					return;

				m_Gain = value;

				OnGainChanged.Raise(this, new FloatEventArgs(m_Gain));
			}
		}

		[PublicAPI]
		public bool Invert
		{
			get
			{
				return m_Invert;
			}
			private set
			{
				if (value == m_Invert)
					return;

				m_Invert = value;

				OnInvertChanged.Raise(this, new BoolEventArgs(m_Invert));
			}
		}

		[PublicAPI]
		public float Level
		{
			get { return m_Level; }
			private set
			{
				if (value == m_Level)
					return;

				m_Level = value;

				OnLevelChanged.Raise(this, new FloatEventArgs(m_Level));
			}
		}

		[PublicAPI]
		public bool Mute
		{
			get { return m_Mute; }
			private set
			{
				if (value != m_Mute)
					return;

				m_Mute = value;

				OnMuteChanged.Raise(this, new BoolEventArgs(m_Mute));
			}
		}

		public float AttributeMinLevel { get { return 0.0f; } }

		public float AttributeMaxLevel { get { return 66.0f; } }

		[PublicAPI]
		public bool PeakOccurring
		{
			get { return m_PeakOccurring; }
			private set
			{
				if (value == m_PeakOccurring)
					return;

				m_PeakOccurring = value;

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

				OnPhantomPowerChanged.Raise(this, new BoolEventArgs(m_PhantomPower));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public AudioInputChannel(AudioInputBlock parent, int index)
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
			OnLevelChanged = null;
			OnInvertChanged = null;
			OnMuteChanged = null;
			OnPhantomPowerChanged = null;
			OnPeakOccurringChanged = null;

			base.Dispose();

			// Unsubscribe
			RequestAttribute(PeakOccurringFeedback, AttributeCode.eCommand.Unsubscribe, PEAK_OCCURRING_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(GainFeedback, AttributeCode.eCommand.Get, GAIN_ATTRIBUTE, null, Index);
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Get, LEVEL_ATTRIBUTE, null, Index);
			RequestAttribute(InvertFeedback, AttributeCode.eCommand.Get, INVERT_ATTRIBUTE, null, Index);
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Get, MUTE_ATTRIBUTE, null, Index);
			RequestAttribute(PeakOccurringFeedback, AttributeCode.eCommand.Get, PEAK_OCCURRING_ATTRIBUTE, null, Index);
			RequestAttribute(PhantomPowerOnFeedback, AttributeCode.eCommand.Get, PHANTOM_POWER_ON_ATTRIBUTE, null, Index);

			// Subscribe
			RequestAttribute(PeakOccurringFeedback, AttributeCode.eCommand.Subscribe, PEAK_OCCURRING_ATTRIBUTE, null, Index);
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

		/// <summary>
		/// Sets the invert state for the given channel.
		/// </summary>
		/// <param name="invert"></param>
		[PublicAPI]
		public void SetInvert(bool invert)
		{
			RequestAttribute(InvertFeedback, AttributeCode.eCommand.Set, INVERT_ATTRIBUTE, new Value(invert), Index);
		}

		/// <summary>
		/// Sets the level for the channel in dB (0 - 66 dB).
		/// </summary>
		/// <param name="level"></param>
		[PublicAPI]
		public void SetLevel(float level)
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Set, LEVEL_ATTRIBUTE, new Value(level), Index);
		}

		/// <summary>
		/// Increments the level in 6dB increments to 66dB.
		/// </summary>
		[PublicAPI]
		public void IncrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Increment, LEVEL_ATTRIBUTE, null, Index);
		}

		/// <summary>
		/// Decrements the level in 6dB increments to 6dB.
		/// </summary>
		[PublicAPI]
		public void DecrementLevel()
		{
			RequestAttribute(LevelFeedback, AttributeCode.eCommand.Decrement, LEVEL_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetMute(bool mute)
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Set, MUTE_ATTRIBUTE, new Value(mute), Index);
		}

		[PublicAPI]
		public void ToggleMute()
		{
			RequestAttribute(MuteFeedback, AttributeCode.eCommand.Toggle, MUTE_ATTRIBUTE, null, Index);
		}

		[PublicAPI]
		public void SetPhantomPower(bool power)
		{
			RequestAttribute(PhantomPowerOnFeedback, AttributeCode.eCommand.Set, PHANTOM_POWER_ON_ATTRIBUTE, new Value(power), Index);
		}

		[PublicAPI]
		public void TogglePhantomPower()
		{
			RequestAttribute(PhantomPowerOnFeedback, AttributeCode.eCommand.Toggle, PHANTOM_POWER_ON_ATTRIBUTE, null, Index);
		}

		#endregion

		#region Subscription Callbacks

		private void GainFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Gain = (value["value"] as Value).FloatValue;
		}

		private void LevelFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Level = (value["value"] as Value).FloatValue;
		}

		private void InvertFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Invert = (value["value"] as Value).BoolValue;
		}

		private void MuteFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Mute = (value["value"] as Value).BoolValue;
		}

		private void PeakOccurringFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			PeakOccurring = (value["value"] as Value).BoolValue;
		}

		private void PhantomPowerOnFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			PhantomPower = (value["value"] as Value).BoolValue;
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
			addRow("Invert", Invert);
			addRow("Level", Level);
			addRow("Mute", Mute);
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

			yield return new GenericConsoleCommand<float>("SetLevel", "SetLevel <DB>", f => SetLevel(f));
			yield return new ConsoleCommand("IncrementLevel", "Increments the level 6 dB", () => IncrementLevel());
			yield return new ConsoleCommand("DecrementLevel", "Decrements the level 6 dB", () => DecrementLevel());

			yield return new GenericConsoleCommand<bool>("SetInvert", "SetInvert <true/false>", b => SetInvert(b));

			yield return new GenericConsoleCommand<bool>("SetMute", "SetMute <true/false>", b => SetMute(b));
			yield return new ConsoleCommand("ToggleMute", "Toggles the current mute state", () => ToggleMute());

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
