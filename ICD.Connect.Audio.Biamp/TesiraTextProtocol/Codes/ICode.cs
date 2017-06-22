using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
{
	public interface ICode : ISerialData
	{
		/// <summary>
		/// Gets the instance tag.
		/// </summary>
		string InstanceTag { get; }
	}
}
