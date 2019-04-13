using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc
{
	public interface IRpc : ISerialData
	{
		string Method { get; }

		string Id { get; set; }
	}
}
