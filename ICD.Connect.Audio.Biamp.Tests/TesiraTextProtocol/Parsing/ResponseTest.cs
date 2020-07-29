using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;
using NUnit.Framework;

namespace ICD.Connect.Audio.Biamp.Tests.TesiraTextProtocol.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class ResponseTest
	{
		private const string ERR = @"-ERR address not found: {""deviceId"":0 ""classCode"":0 ""instanceNum"":0}";

		private const string OK1 = @"+OK ""value"":0.000000";
        private const string OK2 = @"+OK ""time"":""12:00"" ""number"":""503-367-3568"" ""line"":""2""";

        private const string FEEDBACK = @"! ""publishToken"":""my meter"" ""value"":-100.000000";
        private const string FEEDBACK_SPACE = @"! ""publishToken"":INVALID TEST ""value"":-100.000000";

        [Test, UsedImplicitly]
		public void ResponseTypeTest()
		{
			Response err = Response.Deserialize(ERR);
			Response ok1 = Response.Deserialize(OK1);
			Response ok2 = Response.Deserialize(OK2);
			Response feedback = Response.Deserialize(FEEDBACK);

			Assert.AreEqual(Response.eResponseType.Error, err.ResponseType);
			Assert.AreEqual(Response.eResponseType.Success, ok1.ResponseType);
			Assert.AreEqual(Response.eResponseType.Success, ok2.ResponseType);
			Assert.AreEqual(Response.eResponseType.Feedback, feedback.ResponseType);
		}

		[Test, UsedImplicitly]
		public void MessageTest()
		{
			Response err = Response.Deserialize(ERR);
			Response ok1 = Response.Deserialize(OK1);
			Response ok2 = Response.Deserialize(OK2);
			Response feedback = Response.Deserialize(FEEDBACK);

			Assert.AreEqual(@"address not found:", err.Message);
			Assert.AreEqual(string.Empty, ok1.Message);
			Assert.AreEqual(string.Empty, ok2.Message);
			Assert.AreEqual(string.Empty, feedback.Message);
		}

		[Test, UsedImplicitly]
		public void ValuesTest()
		{
			Response err = Response.Deserialize(ERR);
			Response ok1 = Response.Deserialize(OK1);
			Response ok2 = Response.Deserialize(OK2);
			Response feedback = Response.Deserialize(FEEDBACK);

			Assert.Throws<KeyNotFoundException>(() => { IValue a = err.Values.GetValue<Value>("test"); });

			Assert.AreEqual(0, err.Values.GetValue<Value>("deviceId").IntValue);
			Assert.AreEqual(0, err.Values.GetValue<Value>("classCode").IntValue);
			Assert.AreEqual(0, err.Values.GetValue<Value>("instanceNum").IntValue);

			Assert.AreEqual(0.0f, ok1.Values.GetValue<Value>("value").FloatValue);

			Assert.AreEqual("12:00", ok2.Values.GetValue<Value>("time").StringValue);
			Assert.AreEqual("503-367-3568", ok2.Values.GetValue<Value>("number").StringValue);
			Assert.AreEqual("2", ok2.Values.GetValue<Value>("line").StringValue);

			Assert.AreEqual("my meter", feedback.Values.GetValue<Value>("publishToken").StringValue);
			Assert.AreEqual(-100.0f, feedback.Values.GetValue<Value>("value").FloatValue);
		}

        [Test, UsedImplicitly]
        public void FeedbackSpaceTest()
        {
            Response feedback = Response.Deserialize(FEEDBACK_SPACE);

            Assert.AreEqual(Response.eResponseType.Feedback, feedback.ResponseType);
            Assert.AreEqual(string.Empty, feedback.Message);
            Assert.AreEqual("INVALID TEST", feedback.Values.GetValue<Value>("publishToken").GetObjectValue(s => s));
            Assert.AreEqual(-100.0f, feedback.Values.GetValue<Value>("value").FloatValue);
        }
	}
}