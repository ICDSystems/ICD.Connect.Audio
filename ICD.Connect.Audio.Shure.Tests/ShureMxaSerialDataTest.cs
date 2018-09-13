using NUnit.Framework;

namespace ICD.Connect.Audio.Shure.Tests
{
	[TestFixture]
	public sealed class ShureMxaSerialDataTest
	{
		[TestCase(null)]
		[TestCase("test")]
		public void TypeTest(string type)
		{
			ShureMxaSerialData data = new ShureMxaSerialData
			{
				Type = type
			};

			Assert.AreEqual(type, data.Type);
		}

		[TestCase(null)]
		[TestCase(1)]
		public void ChannelTest(int? channel)
		{
			ShureMxaSerialData data = new ShureMxaSerialData
			{
				Channel = channel
			};

			Assert.AreEqual(channel, data.Channel);
		}

		[TestCase(null)]
		[TestCase("test")]
		public void CommandTest(string command)
		{
			ShureMxaSerialData data = new ShureMxaSerialData
			{
				Command = command
			};

			Assert.AreEqual(command, data.Command);
		}

		[TestCase(null)]
		[TestCase("test")]
		public void ValueTest(string value)
		{
			ShureMxaSerialData data = new ShureMxaSerialData
			{
				Value = value
			};

			Assert.AreEqual(value, data.Value);
		}

		[TestCase("SET", null, "LED_BRIGHTNESS", "2", "< SET LED_BRIGHTNESS 2 >")]
		public void SerializeTest(string type, int? channel, string command, string value, string expected)
		{
			ShureMxaSerialData data = new ShureMxaSerialData
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
			ShureMxaSerialData result = ShureMxaSerialData.Deserialize(data);

			Assert.AreEqual(type, result.Type);
			Assert.AreEqual(channel, result.Channel);
			Assert.AreEqual(command, result.Command);
			Assert.AreEqual(value, result.Value);
		}
	}
}
