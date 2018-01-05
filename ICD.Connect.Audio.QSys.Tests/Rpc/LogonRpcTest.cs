using NUnit.Framework;
using ICD.Connect.Audio.QSys.Rpc;

namespace ICD.Connect.Audio.QSys.Tests.Rpc
{
	[TestFixture]
    public sealed class LogonRpcTest : AbstractRpcTest
    {
		[Test]
	    public override void SerializeTest()
	    {
			LogonRpc rpc = new LogonRpc
			{
				Username = "Test1",
				Password = "Test2"
			};

		    string serial = rpc.Serialize();

		    Assert.Inconclusive();
	    }
    }
}
