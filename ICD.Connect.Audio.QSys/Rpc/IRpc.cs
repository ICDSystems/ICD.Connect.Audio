﻿using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.QSys.Rpc
{
	public interface IRpc : ISerialData
	{
		string Method { get; }

		string Id { get; set; }
	}
}