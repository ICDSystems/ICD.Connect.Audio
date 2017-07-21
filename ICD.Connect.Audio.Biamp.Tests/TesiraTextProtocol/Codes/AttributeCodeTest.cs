using NUnit.Framework;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Common.Properties;

namespace ICD.SimplSharp.BiampTesira.Tests.TesiraTextProtocol.Codes
{
	[TestFixture, UsedImplicitly]
	public sealed class AttributeCodeTest
	{
		[Test, UsedImplicitly]
		public void InstanceTagTest()
		{
			AttributeCode code = AttributeCode.Set("test", null, null);
			Assert.AreEqual("test", code.InstanceTag);
		}

		[Test, UsedImplicitly]
		public void IndicesTest()
		{
			AttributeCode code = AttributeCode.Set(null, null, null);
			Assert.AreEqual(0, code.Indices.Length);

			code = AttributeCode.Set(null, null, null, 0, 1, 2);
			Assert.AreEqual(3, code.Indices.Length);
			Assert.AreEqual(0, code.Indices[0]);
			Assert.AreEqual(1, code.Indices[1]);
			Assert.AreEqual(2, code.Indices[2]);
		}

		[Test, UsedImplicitly]
		public void ValueTest()
		{
			AttributeCode code = AttributeCode.Set(null, null, new Value("test"));
			Assert.AreEqual("test", (code.Value as Value).StringValue);
		}

		[Test, UsedImplicitly]
		public void CommandTest()
		{
			AttributeCode code = AttributeCode.Set(null, null, null);
			Assert.AreEqual(AttributeCode.eCommand.Set, code.Command);
		}

		[Test, UsedImplicitly]
		public void AttributeTest()
		{
			AttributeCode code = AttributeCode.Set(null, "test", null);
			Assert.AreEqual("test", code.Attribute);
		}

		[Test, UsedImplicitly]
		public void ToSerialTest()
		{
			AttributeCode code = AttributeCode.Set("Test Instance", "testAttribute", new Value("test"), 1, 2, 3);
			Assert.AreEqual("\"Test Instance\" set testAttribute 1 2 3 \"test\"\n", code.Serialize());
		}
	}
}