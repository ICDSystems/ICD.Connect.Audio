namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	public abstract class AbstractValue<T> : IValue
		where T : AbstractValue<T>
	{
		/// <summary>
		/// Serializes the value to a string in TTP format.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool IValue.CompareEquality(IValue other)
		{
			return CompareEquality(other as T);
		}

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		protected abstract bool CompareEquality(T other);
	}
}
