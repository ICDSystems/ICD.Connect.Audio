using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes
{
	public interface ICode : ISerialData
	{
		/// <summary>
		/// Gets the instance tag.
		/// </summary>
		string InstanceTag { get; }

		/// <summary>
		/// Returns true if the code is equal to the given other code.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool CompareEquality(ICode other);
	}
}
