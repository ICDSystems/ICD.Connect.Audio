using ICD.Common.Utils.EventArguments;
using NUnit.Framework;
using System.Collections.Generic;

namespace ICD.Connect.Audio.Biamp.Tests
{
    [TestFixture]
    public sealed class BiampTesiraSerialBufferTest
    {
        private const string RESPONSE_SERIALIZED = @"+OK ""value"":{""schemaVersion"":2 ""hostname"":""TesiraServer91"" ""defaultGatewayStatus"":""0.0.0.0"" ""networkInterfaceStatusWithName"":[{""interfaceId"":""control"" ""networkInterfaceStatus"":{""macAddress"":""00:90:5e:13:3b:27"" ""linkStatus"":LINK_1_GB ""addressSource"":STATIC ""ip"":""10.30.150.62"" ""netmask"":""255.255.0.0"" ""dhcpLeaseObtainedDate"":"""" ""dhcpLeaseExpiresDate"":"""" ""gateway"":""0.0.0.0""}}] ""dnsStatus"":{""primaryDNSServer"":""0.0.0.0"" ""secondaryDNSServer"":""0.0.0.0"" ""domainName"":""""} ""mDNSEnabled"":true ""telnetDisabled"":false}" + "\x0D\x0A";

        [Test]
        public void ParseResponseTest()
        {
            List<StringEventArgs> results = new List<StringEventArgs>();

            BiampTesiraSerialBuffer buffer = new BiampTesiraSerialBuffer();
            buffer.OnCompletedSerial += (s, e) => results.Add(e);

            buffer.Enqueue(RESPONSE_SERIALIZED);

            Assert.AreEqual(1, results.Count);
        }
    }
}
