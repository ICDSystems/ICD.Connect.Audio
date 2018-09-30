using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using NUnit.Framework;

namespace ICD.Connect.Audio.Biamp.Tests.TesiraTextProtocol.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class ArrayValueTest
	{
		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			// Build a hierarchy of values
			Value childValue = new Value(10);

			Value childControlValueA = new Value(1);
			Value childControlValueB = new Value(2);
			ControlValue childControl =
				new ControlValue(new Dictionary<string, IValue>
				{
					{"A", childControlValueA},
					{"B", childControlValueB}
				});

			Value childArrayValueA = new Value(1);
			Value childArrayValueB = new Value(2);
			ArrayValue childArray = new ArrayValue(new IValue[] {childArrayValueA, childArrayValueB});

			ArrayValue array = new ArrayValue(new IValue[] { childValue, childControl, childArray });

			// Serialize and deserialize
			string serialized = array.Serialize();
			ArrayValue deserialized = ArrayValue.Deserialize(serialized);

			// Compare results
			Assert.AreEqual(array.Count, deserialized.Count);

			Value deserializedChildValue = deserialized[0] as Value;
			Assert.AreEqual(childValue.IntValue, deserializedChildValue.IntValue);

			ControlValue deserializedChildControl = deserialized[1] as ControlValue;
			Value cdeserializedChildControlValueA = deserializedChildControl.GetValue<Value>("A");
			Value deserializedChildControlValueB = deserializedChildControl.GetValue<Value>("B");
			Assert.AreEqual(childControlValueA.IntValue, cdeserializedChildControlValueA.IntValue);
			Assert.AreEqual(childControlValueB.IntValue, deserializedChildControlValueB.IntValue);

			ArrayValue deserializedChildArray = deserialized[2] as ArrayValue;
			Value deserializedChildArrayValueA = deserializedChildArray[0] as Value;
			Value deserializedChildArrayValueB = deserializedChildArray[1] as Value;
			Assert.AreEqual(childArrayValueA.IntValue, deserializedChildArrayValueA.IntValue);
			Assert.AreEqual(childArrayValueB.IntValue, deserializedChildArrayValueB.IntValue);
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string serialized = @"[10 {""A"":1 ""B"":2} [1 2]]";
			ArrayValue deserialized = ArrayValue.Deserialize(serialized);

			Assert.AreEqual(3, deserialized.Count);

			Value deserializedChildValue = deserialized[0] as Value;
			Assert.AreEqual(10, deserializedChildValue.IntValue);

			ControlValue deserializedChildControl = deserialized[1] as ControlValue;
            Value cdeserializedChildControlValueA = deserializedChildControl.GetValue<Value>("A");
            Value deserializedChildControlValueB = deserializedChildControl.GetValue<Value>("B");
            Assert.AreEqual(1, cdeserializedChildControlValueA.IntValue);
			Assert.AreEqual(2, deserializedChildControlValueB.IntValue);

			ArrayValue deserializedChildArray = deserialized[2] as ArrayValue;
			Value deserializedChildArrayValueA = deserializedChildArray[0] as Value;
			Value deserializedChildArrayValueB = deserializedChildArray[1] as Value;
			Assert.AreEqual(1, deserializedChildArrayValueA.IntValue);
			Assert.AreEqual(2, deserializedChildArrayValueB.IntValue);
		}
	}
}
