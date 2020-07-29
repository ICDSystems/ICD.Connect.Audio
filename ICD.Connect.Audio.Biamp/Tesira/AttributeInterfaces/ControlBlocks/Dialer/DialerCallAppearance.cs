using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.API.Commands;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.ControlBlocks.Dialer
{
	public sealed class DialerCallAppearance : AbstractAttributeChild<DialerLine>
	{
		private const string SPEED_DIAL_SERVICE ="speedDial";
		private const string REDIAL_SERVICE ="redial";
		private const string END_SERVICE ="end";
		private const string FLASH_SERVICE ="flash";
		private const string SEND_SERVICE ="send";
		private const string DIAL_SERVICE ="dial";
		private const string ANSWER_SERVICE ="answer";
		private const string CONFERENCE_SERVICE ="lconf";
		private const string RESUME_SERVICE ="resume";
		private const string LEAVE_CONFERENCE_SERVICE ="leaveConf";
		private const string SPECIFY_CALL_APPEARANCE_SERVICE ="callAppearance";
		private const string HOLD_SERVICE ="hold";
		private const string OFF_HOOK_SERVICE ="offHook";
		private const string ON_HOOK_SERVICE ="onHook";

		#region Properties

		private int Line { get { return Parent.Index; } }

		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected override string IndexName { get { return "CallAppearance"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		public DialerCallAppearance(DialerLine parent, int index)
			: base(parent, index)
		{
			if (Device.Initialized)
				Initialize();
		}

		#region Services

		[PublicAPI]
		public void SpeedDial(int speedDialerEntry)
		{
			RequestService(SPEED_DIAL_SERVICE, new Value(speedDialerEntry), Line, Index);
		}

		[PublicAPI]
		public void Redial()
		{
			RequestService(REDIAL_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void End()
		{
			RequestService(END_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Flash()
		{
			RequestService(FLASH_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Send()
		{
			RequestService(SEND_SERVICE, null, Line, Index);
		}

		/// <summary>
		/// Dials the given number. Used only when on-hook.
		/// </summary>
		/// <param name="number"></param>
		[PublicAPI]
		public void Dial(string number)
		{
			RequestService(DIAL_SERVICE, new Value(number), Line, Index);
		}

		[PublicAPI]
		public void Answer()
		{
			RequestService(ANSWER_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Conference()
		{
			RequestService(CONFERENCE_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Resume()
		{
			RequestService(RESUME_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void LeaveConference()
		{
			RequestService(LEAVE_CONFERENCE_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void SpecifyCallAppearance()
		{
			RequestService(SPECIFY_CALL_APPEARANCE_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void Hold()
		{
			RequestService(HOLD_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void GoOffHook()
		{
			RequestService(OFF_HOOK_SERVICE, null, Line, Index);
		}

		[PublicAPI]
		public void GoOnHook()
		{
			RequestService(ON_HOOK_SERVICE, null, Line, Index);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("SpeedDial","SpeedDial <ID>", i => SpeedDial(i));
			yield return new ConsoleCommand("Redial","Redial", () => Redial());
			yield return new ConsoleCommand("End", "End", () => End());
			yield return new ConsoleCommand("Flash", "Flash", () => Flash());
			yield return new ConsoleCommand("Send", "Send", () => Send());
			yield return new GenericConsoleCommand<string>("Dial", "Redial <NUMBER>", s => Dial(s));
			yield return new ConsoleCommand("Answer", "Answer", () => Answer());
			yield return new ConsoleCommand("Hold", "Hold", () => Hold());
			yield return new ConsoleCommand("Resume", "Resume", () => Resume());
			yield return new ConsoleCommand("Conference", "Conference", () => Conference());
			yield return new ConsoleCommand("LeaveConference", "LeaveConference", () => LeaveConference());
			yield return new ConsoleCommand("GoOffHook", "GoOffHook", () => GoOffHook());
			yield return new ConsoleCommand("GoOnHook", "GoOnHook", () => GoOnHook());
		}

		/// <summary>
		/// Workaround for"unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
