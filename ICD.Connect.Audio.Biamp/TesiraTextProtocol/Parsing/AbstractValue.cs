namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	public abstract class AbstractValue
	{
		/// <summary>
		/// Serializes the value to a string in TTP format.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();
	}
}
