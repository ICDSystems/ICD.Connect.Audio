using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;
using NUnit.Framework;

namespace ICD.Connect.Audio.QSys.Tests.Devices.QSysCore.Rpc
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
