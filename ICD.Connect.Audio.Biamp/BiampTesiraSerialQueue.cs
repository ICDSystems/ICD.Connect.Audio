using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Audio.Biamp
{
	/// <summary>
	/// The Tesira doesn't tell us what it is responding to on the same line. This means
	/// we need to send a command, wait for the response, and raise the Rx and Tx as a
	/// pair for event handling.
	/// 
	/// The Tesira is especially annoying because it will also send us subscription updates
	/// which have NO Tx to pair with.
	/// 
	/// Feedback looks something like:
	/// 
	///		Mixer4 get outputLevel
	///		+OK "value":0.000000
	/// 
	///		MIxer4 subscribe outputLevel
	///		-ERR address not found: {"deviceId":0 "classCode":0 "instanceNum":0}
	/// 
	///		Mixer4 subscribe outputLevel
	///		! "publishToken":"" "value":0.000000
	///		+OK
	/// 
	///		Mixer4 set outputLevel 1
	///		! "publishToken":"" "value":1.000000
	///		+OK
	/// 
	///		Mixer4 set outputMute true
	///		+OK
	/// </summary>
	public sealed class BiampTesiraSerialQueue : AbstractSerialQueue
	{
		private const long RATE_LIMIT = 150;

		/// <summary>
		/// Raised when the biamp sends subscription feedback data.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSubscriptionFeedback;

		private readonly IcdTimer m_DelayTimer;

		/// <summary>
		/// Constructor.
		/// </summary>
		public BiampTesiraSerialQueue()
		{
			m_DelayTimer = new IcdTimer(RATE_LIMIT);
			m_DelayTimer.OnElapsed += CommandDelayCallback;
		}

		public override void Dispose()
		{
			OnSubscriptionFeedback = null;

			m_DelayTimer.Dispose();

			base.Dispose();
		}

		#region Private Methods

		private void CommandDelayCallback(object sender, EventArgs args)
		{
			HandleNextCommand();
		}

		protected override void CommandAdded()
		{
			HandleNextCommand();
		}

		private void HandleNextCommand()
		{
			if (!IsCommandInProgress && CommandCount > 0)
				SendImmediate();

			if (CommandCount == 0)
				m_DelayTimer.Stop();
			else
				m_DelayTimer.Restart(RATE_LIMIT);
		}

		protected override void CommandFinished()
		{
			m_DelayTimer.Restart(RATE_LIMIT);
		}

		/// <summary>
		/// Called when the buffer completes a string.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="args"></param>
		protected override void BufferCompletedSerial(object buffer, StringEventArgs args)
		{
			// Handle telnet negotiation
			if (args.Data.StartsWith((char)TelnetControl.HEADER))
			{
				string rejection = TelnetControl.Reject(args.Data);
				Port.Send(rejection);
				return;
			}

			// Handle subscription feedback seperately
			if (args.Data.StartsWith(Response.FEEDBACK))
			{
				try
				{
					OnSubscriptionFeedback.Raise(this, new StringEventArgs(args.Data));
				}
				catch (Exception e)
				{
					ServiceProvider.GetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, e, "Failed to raise subscription feedback event - {0}", e.Message);
				}
				return;
			}

			// Ignore any messages that dont fit expected pattern
			if (args.Data.StartsWith(Response.CANNOT_DELIVER) ||
			    args.Data.StartsWith(Response.ERROR) ||
			    args.Data.StartsWith(Response.GENERAL_FAILURE) ||
			    args.Data.StartsWith(Response.SUCCESS))
				base.BufferCompletedSerial(buffer, args);
		}

		#endregion
	}
}
