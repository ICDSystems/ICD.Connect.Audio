using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;
using NUnit.Framework;

namespace ICD.Connect.Audio.Biamp.Tests.TesiraTextProtocol.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class ValueTest
	{
		[Test, UsedImplicitly]
		public void IsStringTest()
		{
			Assert.IsTrue(new Value("test").IsString);
			Assert.IsFalse(new Value(0).IsString);
		}

		[Test, UsedImplicitly]
		public void IntValueTest()
		{
			Assert.Throws<FormatException>(() => { int test = new Value("test").IntValue; });

			Assert.AreEqual(0, new Value(0).IntValue);
			Assert.AreEqual(100, new Value(100).IntValue);
		}

		[Test, UsedImplicitly]
		public void FloatValueTest()
		{
			Assert.Throws<FormatException>(() => { float test = new Value("test").FloatValue; });

			Assert.AreEqual(0.0f, new Value(0.0f).FloatValue);
			Assert.AreEqual(100.0f, new Value(100.0f).FloatValue);
		}

		[Test, UsedImplicitly]
		public void StringValueTest()
		{
			Assert.Throws<FormatException>(() => { string test = new Value(10).StringValue; });

			Assert.AreEqual("test", new Value("test").StringValue);
		}

		[UsedImplicitly]
		[TestCase("test", "\"test\"")]
		public void SerializeTest(string data, string expected)
		{
			Assert.AreEqual(expected, new Value(data).Serialize());
		}

		[Test]
		public void GetStringValuesTest()
		{
			const string data = @"\""01131947\""\""test\""\""\""";
			string[] expected = {"01131947", "test", ""};

			Value value = new Value(data);
			string[] result = value.GetStringValues().ToArray();

			Assert.IsTrue(expected.SequenceEqual(result));
		}
	}
}
