using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.Services
{
	public sealed class SessionService : AbstractService
	{
		public const string INSTANCE_TAG = "SESSION";

		private const string ALIASES_ATTRIBUTE = "aliases";
		private const string VERBOSE_OUTPUT_ENABLED_ATTRIBUTE = "verbose";

		[PublicAPI]
		public event EventHandler OnAliasesChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnVerboseOutputEnabledChanged; 

		private readonly List<string> m_Aliases;
		private readonly SafeCriticalSection m_AliasesSection;

		private bool m_VerboseOutputEnabled;

		#region Properties

		[PublicAPI]
		public bool VerboseOutputEnabled
		{
			get { return m_VerboseOutputEnabled; }
			private set
			{
				if (value == m_VerboseOutputEnabled)
					return;

				m_VerboseOutputEnabled = value;

				Log(eSeverity.Informational, "VerboseOutputEnabled set to {0}", m_VerboseOutputEnabled);

				OnVerboseOutputEnabledChanged.Raise(this, new BoolEventArgs(m_VerboseOutputEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		public SessionService(BiampTesiraDevice device)
			: base(device, INSTANCE_TAG)
		{
			m_Aliases = new List<string>();
			m_AliasesSection = new SafeCriticalSection();

			if (device.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnAliasesChanged = null;
			OnVerboseOutputEnabledChanged = null;

			base.Dispose();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Get initial values
			RequestAttribute(AliasesFeedback, AttributeCode.eCommand.Get, ALIASES_ATTRIBUTE, null);
			RequestAttribute(VerboseOutputEnabledFeedback, AttributeCode.eCommand.Get, VERBOSE_OUTPUT_ENABLED_ATTRIBUTE, null);
		}

		[PublicAPI]
		public IEnumerable<string> GetAliases()
		{
			return m_AliasesSection.Execute(() => m_Aliases.ToArray());
		}

		[PublicAPI]
		public void SetVerboseOutputEnabled(bool enabled)
		{
			RequestAttribute(VerboseOutputEnabledFeedback, AttributeCode.eCommand.Set, VERBOSE_OUTPUT_ENABLED_ATTRIBUTE, new Value(enabled));
		}

		[PublicAPI]
		public void ToggleVerboseOutputEnabled()
		{
			RequestAttribute(VerboseOutputEnabledFeedback, AttributeCode.eCommand.Toggle, VERBOSE_OUTPUT_ENABLED_ATTRIBUTE, null);
		}

		#endregion

		#region Subscription Callbacks

		private void AliasesFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			ArrayValue array = value.GetValue<ArrayValue>("list");

			m_AliasesSection.Enter();

			try
			{
				foreach (Value child in array.Cast<Value>())
					m_Aliases.Add(child.StringValue);
				m_Aliases.Clear();
			}
			finally
			{
				m_AliasesSection.Leave();
			}

			OnAliasesChanged.Raise(this);
		}

		private void VerboseOutputEnabledFeedback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			VerboseOutputEnabled = innerValue.BoolValue;
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

			addRow("Verbose Output Enabled", VerboseOutputEnabled);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetVerboseOutputEnabled", "SetVerboseOutputEnabled <true/false>",
			                                             b => SetVerboseOutputEnabled(b));
			yield return new ConsoleCommand("ToggleVerboseOutputEnabled", "", () => ToggleVerboseOutputEnabled());

			yield return new ConsoleCommand("PrintAliases", "", () => PrintAliases());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string PrintAliases()
		{
			return string.Join(IcdEnvironment.NewLine, GetAliases().ToArray());
		}

		#endregion
	}
}
