using NUnit.Framework;

namespace ICD.Connect.Audio.Shure.Tests
{
	[TestFixture]
	public sealed class ShureMicSerialDataTest
	{
		[TestCase(null)]
		[TestCase("test")]
		public void TypeTest(string type)
		{
			ShureMicSerialData data = new ShureMicSerialData
			{
				Type = type
			};

			Assert.AreEqual(type, data.Type);
		}

		[TestCase(null)]
		[TestCase(1)]
		public void ChannelTest(int? channel)
		{
			ShureMicSerialData data = new ShureMicSerialData
			{
				Channel = channel
			};

			Assert.AreEqual(channel, data.Channel);
		}

		[TestCase(null)]
		[TestCase("test")]
		public void CommandTest(string command)
		{
			ShureMicSerialData data = new ShureMicSerialData
			{
				Command = command
			};

			Assert.AreEqual(command, data.Command);
		}

		[TestCase(null)]
		[TestCase("test")]
		public void ValueTest(string value)
		{
			ShureMicSerialData data = new ShureMicSerialData
			{
				Value = value
			};

			Assert.AreEqual(value, data.Value);
		}

		[TestCase("SET", null, "LED_BRIGHTNESS", "2", "< SET LED_BRIGHTNESS 2 >")]
		public void SerializeTest(string type, int? channel, string command, string value, string expected)
		{
			ShureMicSerialData data = new ShureMicSerialData
			{
				Type = type,
				Channel = channel,
				Command = command,
				Value = value
			};

			Assert.AreEqual(expected, data.Serialize());
		}

		[TestCase("< SET LED_BRIGHTNESS 2 >", "SET", null, "LED_BRIGHTNESS", "2")]
		public void DeserializeTest(string data, string type, int? channel, string command, string value)
		{
			ShureMicSerialData result = ShureMicSerialData.Deserialize(data);

			Assert.AreEqual(type, result.Type);
			Assert.AreEqual(channel, result.Channel);
			Assert.AreEqual(command, result.Command);
			Assert.AreEqual(value, result.Value);
		}
	}
}
