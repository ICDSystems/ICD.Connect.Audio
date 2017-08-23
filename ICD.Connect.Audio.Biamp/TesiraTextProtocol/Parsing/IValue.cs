namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	public interface IValue
	{
		/// <summary>
		/// Serializes the value to a string in TTP format.
		/// </summary>
		/// <returns></returns>
		string Serialize();

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool CompareEquality(IValue other);
	}
}
