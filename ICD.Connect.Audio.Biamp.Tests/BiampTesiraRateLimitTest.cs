using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Mock.Ports;
using ICD.Connect.Protocol.SerialQueues;
using NUnit.Framework;

namespace ICD.Connect.Audio.Biamp.Tests
{
	[TestFixture]
	public sealed class BiampTesiraRateLimitTest
	{
		private const string COMMAND_1 = "ATC_RX_LVL get rampStep 1\n";
		private const string COMMAND_2 = "PRIVACY_MUTE get numChannels\n";
		private const string COMMAND_3 = "PRIVACY_MUTE get ganged\n";

		private const string RESPONSE_1 = "+OK \"value\":1.000000\r\n";
		private const string RESPONSE_2 = "+OK \"value\":1\r\n";
		private const string RESPONSE_3 = "+OK \"value\":false\r\n";

		/// <summary>
		/// Tests to make sure that after parsing a response, the queue does not transmit until the cooldown period expires.
		/// </summary>
		[Test]
		public void RateLimitTest()
		{
			ILoggerService logger = ServiceProvider.GetService<ILoggerService>();
			SerialQueue serialQueue = new SerialQueue()
			{
				CommandDelayTime = BiampTesiraDevice.COMMAND_DELAY_MS,
				Timeout = BiampTesiraDevice.TIMEOUT_MS
			};
			MockSerialPort serialPort = new MockSerialPort();
			serialQueue.SetPort(serialPort);
			BiampTesiraSerialBuffer buffer = new BiampTesiraSerialBuffer();
			serialQueue.SetBuffer(buffer);

			serialPort.Connect();

			DateTime lastReceive = default(DateTime);

			serialQueue.OnSerialResponse += (sender, args) =>
			                                {
				                                lastReceive = IcdEnvironment.GetLocalTime();
												logger.AddEntry(eSeverity.Informational, "Command Recieved.");
											};

			serialQueue.OnSerialTransmission += (sender, args) =>
												{
													double elapsedMilliseconds = (IcdEnvironment.GetLocalTime() - lastReceive)
														.TotalMilliseconds;


													Assert.GreaterOrEqual(elapsedMilliseconds, 150,
																		  "Queue did not wait more than 150 ms before sending the next command.");
													logger.AddEntry(eSeverity.Informational, "Command Transmitted.");

													switch (args.Data.Serialize())
													{
														case COMMAND_1:
															serialPort.Receive(RESPONSE_1);
															break;
														case COMMAND_2:
															serialPort.Receive(RESPONSE_2);
															break;
														case COMMAND_3:
															serialPort.Receive(RESPONSE_3);
															break;
														default:
															throw new InvalidOperationException("args.Data contains unknown command");
													}
												};

			serialQueue.Enqueue(new SerialData(COMMAND_1));
			serialQueue.Enqueue(new SerialData(COMMAND_2));
			serialQueue.Enqueue(new SerialData(COMMAND_3));
			ThreadingUtils.Sleep(60000);
		}

		/// <summary>
		/// Tests to make sure that after receiving no response, the queue waits to transmit until the command times out.
		/// </summary>
		[Test]
		public void TimeoutTest()
		{
			ILoggerService logger = ServiceProvider.GetService<ILoggerService>();
			IcdStopwatch stopwatch = new IcdStopwatch();
			SerialQueue serialQueue = new SerialQueue()
			{
				CommandDelayTime = BiampTesiraDevice.COMMAND_DELAY_MS,
				Timeout = BiampTesiraDevice.TIMEOUT_MS
			};
			MockSerialPort serialPort = new MockSerialPort();
			serialQueue.SetPort(serialPort);
			BiampTesiraSerialBuffer buffer = new BiampTesiraSerialBuffer();
			serialQueue.SetBuffer(buffer);
			serialPort.Connect();
			bool transmitting = false;
			serialQueue.OnSerialTransmission += (sender, args) =>
			                                    {
				                                    if (!transmitting)
				                                    {
					                                    transmitting = true;
					                                    stopwatch.Restart();
				                                    }
				                                    else
				                                    {
					                                    Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 20000,
															"Queue did not wait 20 seconds before sending a command when receiving no response.");
				                                    }
				                                    logger.AddEntry(eSeverity.Informational, "Command Transmitted.");
												};
			serialQueue.OnTimeout += (sender, args) =>
			                         {
				                         transmitting = false;
				                         stopwatch.Reset();
										 logger.AddEntry(eSeverity.Informational, "Command Timed Out");
			                         };
			serialQueue.Enqueue(new SerialData(COMMAND_1));
			serialQueue.Enqueue(new SerialData(COMMAND_2));
			serialQueue.Enqueue(new SerialData(COMMAND_3));
			ThreadingUtils.Sleep(65000);
		}
	}
}
