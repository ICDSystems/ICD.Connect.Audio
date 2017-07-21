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
			Assert.Throws<KeyNotFoundException>(() => { object test = a["test"]; });

			Value value = new Value(0);
			ControlValue b = new ControlValue(new Dictionary<string, AbstractValue> {{"test", value}});

			Assert.DoesNotThrow(() => { object test = b["test"]; });
			Assert.AreEqual(value, b["test"]);
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			ControlValue control = ControlValue.Deserialize(CONTROL_SERIALIZED);

			Assert.AreEqual(2, (control["schemaVersion"] as Value).IntValue);
			Assert.AreEqual("TesiraServer91", (control["hostname"] as Value).StringValue);
			Assert.AreEqual("0.0.0.0", (control["defaultGatewayStatus"] as Value).StringValue);
			Assert.AreEqual(true, (control["mDNSEnabled"] as Value).BoolValue);
			Assert.AreEqual(false, (control["telnetDisabled"] as Value).BoolValue);

			ControlValue dnsStatus = control["dnsStatus"] as ControlValue;

			Assert.AreEqual("0.0.0.0", (dnsStatus["primaryDNSServer"] as Value).StringValue);
			Assert.AreEqual("0.0.0.0", (dnsStatus["secondaryDNSServer"] as Value).StringValue);
			Assert.AreEqual("", (dnsStatus["domainName"] as Value).StringValue);

			ArrayValue networkInterfaceStatusWithName = control["networkInterfaceStatusWithName"] as ArrayValue;

			Assert.AreEqual(1, networkInterfaceStatusWithName.Count);

			ControlValue arrayItemControl = networkInterfaceStatusWithName[0] as ControlValue;

			Assert.AreEqual("control", (arrayItemControl["interfaceId"] as Value).StringValue);

			ControlValue newtworkStatus = arrayItemControl["networkInterfaceStatus"] as ControlValue;

			Assert.AreEqual("00:90:5e:13:3b:27", (newtworkStatus["macAddress"] as Value).StringValue);
			//Assert.AreEqual(, (newtworkStatus["linkStatus"] as Value).StringValue);
			//Assert.AreEqual(, (newtworkStatus["addressSource"] as Value).StringValue);
			Assert.AreEqual("10.30.150.62", (newtworkStatus["ip"] as Value).StringValue);
			Assert.AreEqual("255.255.0.0", (newtworkStatus["netmask"] as Value).StringValue);
			Assert.AreEqual("", (newtworkStatus["dhcpLeaseObtainedDate"] as Value).StringValue);
			Assert.AreEqual("", (newtworkStatus["dhcpLeaseExpiresDate"] as Value).StringValue);
			Assert.AreEqual("0.0.0.0", (newtworkStatus["gateway"] as Value).StringValue);
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			// Build a hierarchy of values
			Value childArrayValueA = new Value(1);
			Value childArrayValueB = new Value(2);
			ArrayValue childArray = new ArrayValue(new AbstractValue[] { childArrayValueA, childArrayValueB });

			Value childControlValueA = new Value(1);
			Value childControlValueB = new Value(2);
			ControlValue control =
				new ControlValue(new Dictionary<string, AbstractValue>
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

			Value deserializedControlValueA = deserialized["A"] as Value;
			Value deserializedControlValueB = deserialized["B"] as Value;
			Assert.AreEqual(childControlValueA.IntValue, deserializedControlValueA.IntValue);
			Assert.AreEqual(childControlValueB.IntValue, deserializedControlValueB.IntValue);

			ArrayValue deserializedChildArray = deserialized["Array"] as ArrayValue;
			Value deserializedChildArrayValueA = deserializedChildArray[0] as Value;
			Value deserializedChildArrayValueB = deserializedChildArray[1] as Value;
			Assert.AreEqual(childArrayValueA.IntValue, deserializedChildArrayValueA.IntValue);
			Assert.AreEqual(childArrayValueB.IntValue, deserializedChildArrayValueB.IntValue);
		}
	}
}
