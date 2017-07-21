using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using NUnit.Framework;

namespace ICD.SimplSharp.BiampTesira.Tests.TesiraTextProtocol.Codes
{
	[TestFixture]
	public class ServiceCodeTest
	{
		[Test]
		public void InstanceTagTest()
		{
			ServiceCode code = new ServiceCode("test", null, null);
			Assert.AreEqual("test", code.InstanceTag);
		}

		[Test]
		public void ValueTest()
		{
			ServiceCode code = new ServiceCode(null, null, new Value("test"));
			Assert.AreEqual("test", (code.Value as Value).StringValue);
		}

		[Test]
		public void ServiceTest()
		{
			ServiceCode code = new ServiceCode(null, "test", null);
			Assert.AreEqual("test", code.Service);
		}

		[Test]
		public void ToSerialTest()
		{
			ServiceCode code = new ServiceCode("Test Instance", "testService", new Value("test"));
			Assert.AreEqual("\"Test Instance\" testService \"test\"\n", code.Serialize());
		}
	}
}
