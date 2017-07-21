using System.Linq;
using NUnit.Framework;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.SimplSharp.BiampTesira.Tests.TesiraTextProtocol.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class TtpUtilsTest
	{
		[UsedImplicitly]
		[TestCase("", null, "")]
		[TestCase("\"key\":value", "key", "value")]
		[TestCase("value", null, "value")]
		public void RemoveKeyTest(string kvp, string expectedKey, string expectedValue)
		{
			string key;
			string value = TtpUtils.RemoveKey(kvp, out key);

			Assert.AreEqual(expectedKey, key);
			Assert.AreEqual(expectedValue, value);
		}

		[UsedImplicitly]
		[TestCase("\"test\"", "test")]
		[TestCase("\"test", "test")]
		[TestCase("test\"", "test")]
		public void RemoveQuotes(string quoted, string expected)
		{
			Assert.AreEqual(expected, TtpUtils.RemoveQuotes(quoted));
		}

		[Test, UsedImplicitly]
		public void SplitValues()
		{
			const string test = @"""deviceId"":0 ""classCode"":0 ""instanceNum"":0";
			string[] split = TtpUtils.SplitValues(test).ToArray();

			Assert.AreEqual(@"""deviceId"":0", split[0]);
			Assert.AreEqual(@"""classCode"":0", split[1]);
			Assert.AreEqual(@"""instanceNum"":0", split[2]);
		}
	}
}
