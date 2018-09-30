using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using NUnit.Framework;

namespace ICD.Connect.Audio.Biamp.Tests.TesiraTextProtocol.Parsing
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
		public void RemoveQuotesTest(string quoted, string expected)
		{
			Assert.AreEqual(expected, TtpUtils.RemoveQuotes(quoted));
		}

        [Test, UsedImplicitly]
        public void GetKeyedValuesTest()
        {
            const string data = @"{""a"":1 ""b"":""2"" ""c"":[3]}";

            var kvps = TtpUtils.GetKeyedValues(data).ToArray();

            Assert.AreEqual(3, kvps.Length);
            Assert.AreEqual("a", kvps[0].Key);
            Assert.AreEqual("b", kvps[1].Key);
            Assert.AreEqual("c", kvps[2].Key);
        }

        [Test, UsedImplicitly]
        public void GetArrayValuesTest()
        {
        }

        [Test, UsedImplicitly]
		public void SplitValuesTest()
		{
			const string test = @"""deviceId"":0 ""classCode"":0 ""instanceNum"":0";
			string[] split = TtpUtils.SplitValues(test).ToArray();

			Assert.AreEqual(@"""deviceId"":0", split[0]);
			Assert.AreEqual(@"""classCode"":0", split[1]);
			Assert.AreEqual(@"""instanceNum"":0", split[2]);
		}

        [Test, UsedImplicitly]
        public void SplitValuesSpaceTest()
        {
            const string test = @"""deviceId"":0 ""classCode"":INVALID VALUE ""instanceNum"":0";
            string[] split = TtpUtils.SplitValues(test).ToArray();

            Assert.AreEqual(@"""deviceId"":0", split[0]);
            Assert.AreEqual(@"""classCode"":INVALID VALUE", split[1]);
            Assert.AreEqual(@"""instanceNum"":0", split[2]);
        }
    }
}
