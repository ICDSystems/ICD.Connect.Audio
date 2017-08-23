using System.Collections.Generic;
using NUnit.Framework;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.SimplSharp.BiampTesira.Tests.TesiraTextProtocol.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class ControlValueTest
	{
		private const string CONTROL_SERIALIZED = @"
{
	""schemaVersion"":2
	""hostname"":""TesiraServer91""
	""defaultGatewayStatus"":""0.0.0.0""
	""networkInterfaceStatusWithName"":
	[
		{
			""interfaceId"":""control""
			""networkInterfaceStatus"":
			{
				""macAddress"":""00:90:5e:13:3b:27""
				""linkStatus"":LINK_1_GB
				""addressSource"":STATIC
				""ip"":""10.30.150.62""
				""netmask"":""255.255.0.0""
				""dhcpLeaseObtainedDate"":""""
				""dhcpLeaseExpiresDate"":""""
				""gateway"":""0.0.0.0""
			}
		}
	]
	""dnsStatus"":
	{
		""primaryDNSServer"":""0.0.0.0""
		""secondaryDNSServer"":""0.0.0.0""
		""domainName"":""""
	}
	""mDNSEnabled"":true
	""telnetDisabled"":false
}";
		
		[Test, UsedImplicitly]
		public void KeyTest()
		{
			ControlValue a = new ControlValue();
			Assert.Throws<KeyNotFoundException>(() => { object test = a.GetValue<Value>("test"); });

			Value value = new Value(0);
			ControlValue b = new ControlValue(new Dictionary<string, IValue> {{"test", value}});

			Assert.DoesNotThrow(() => { object test = b.GetValue<Value>("test"); });
			Assert.AreEqual(value, b.GetValue<Value>("test"));
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			ControlValue control = ControlValue.Deserialize(CONTROL_SERIALIZED);

			Assert.AreEqual(2, control.GetValue<Value>("schemaVersion").IntValue);
			Assert.AreEqual("TesiraServer91", control.GetValue<Value>("hostname").StringValue);
			Assert.AreEqual("0.0.0.0", control.GetValue<Value>("defaultGatewayStatus").StringValue);
			Assert.AreEqual(true, control.GetValue<Value>("mDNSEnabled").BoolValue);
			Assert.AreEqual(false, control.GetValue<Value>("telnetDisabled").BoolValue);

			ControlValue dnsStatus = control.GetValue<ControlValue>("dnsStatus");

			Assert.AreEqual("0.0.0.0", dnsStatus.GetValue<Value>("primaryDNSServer").StringValue);
			Assert.AreEqual("0.0.0.0", dnsStatus.GetValue<Value>("secondaryDNSServer").StringValue);
			Assert.AreEqual("", dnsStatus.GetValue<Value>("domainName").StringValue);

			ArrayValue networkInterfaceStatusWithName = control.GetValue<ArrayValue>("networkInterfaceStatusWithName");

			Assert.AreEqual(1, networkInterfaceStatusWithName.Count);

			ControlValue arrayItemControl = networkInterfaceStatusWithName[0] as ControlValue;

			Assert.AreEqual("control", arrayItemControl.GetValue<Value>("interfaceId").StringValue);

			ControlValue newtworkStatus = arrayItemControl.GetValue<ControlValue>("networkInterfaceStatus");

			Assert.AreEqual("00:90:5e:13:3b:27", newtworkStatus.GetValue<Value>("macAddress").StringValue);
			//Assert.AreEqual(, (newtworkStatus["linkStatus"] as Value).StringValue);
			//Assert.AreEqual(, (newtworkStatus["addressSource"] as Value).StringValue);
			Assert.AreEqual("10.30.150.62", newtworkStatus.GetValue<Value>("ip").StringValue);
			Assert.AreEqual("255.255.0.0", newtworkStatus.GetValue<Value>("netmask").StringValue);
			Assert.AreEqual("", newtworkStatus.GetValue<Value>("dhcpLeaseObtainedDate").StringValue);
			Assert.AreEqual("", newtworkStatus.GetValue<Value>("dhcpLeaseExpiresDate").StringValue);
			Assert.AreEqual("0.0.0.0", newtworkStatus.GetValue<Value>("gateway").StringValue);
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			// Build a hierarchy of values
			Value childArrayValueA = new Value(1);
			Value childArrayValueB = new Value(2);
			ArrayValue childArray = new ArrayValue(new IValue[] { childArrayValueA, childArrayValueB });

			Value childControlValueA = new Value(1);
			Value childControlValueB = new Value(2);
			ControlValue control =
				new ControlValue(new Dictionary<string, IValue>
				{
					{"A", childControlValueA},
					{"B", childControlValueB},
					{"Array", childArray}
				});

			// Serialize and deserialize
			string serialized = control.Serialize();
			ControlValue deserialized = ControlValue.Deserialize(serialized);

			// Compare results
			Assert.AreEqual(control.Count, control.Count);

			Value deserializedControlValueA = deserialized.GetValue<Value>("A");
			Value deserializedControlValueB = deserialized.GetValue<Value>("B");
			Assert.AreEqual(childControlValueA.IntValue, deserializedControlValueA.IntValue);
			Assert.AreEqual(childControlValueB.IntValue, deserializedControlValueB.IntValue);

			ArrayValue deserializedChildArray = deserialized.GetValue<ArrayValue>("Array");
			Value deserializedChildArrayValueA = deserializedChildArray[0] as Value;
			Value deserializedChildArrayValueB = deserializedChildArray[1] as Value;
			Assert.AreEqual(childArrayValueA.IntValue, deserializedChildArrayValueA.IntValue);
			Assert.AreEqual(childArrayValueB.IntValue, deserializedChildArrayValueB.IntValue);
		}
	}
}
