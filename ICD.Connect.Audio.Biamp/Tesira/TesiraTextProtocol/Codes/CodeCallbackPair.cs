using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes
{
	/// <summary>
	/// Simple pairing of an ICode and a callback. This lets us execute the callback
	/// when we get a response from the device for the given code.
	/// </summary>
	public sealed class CodeCallbackPair : ICode
	{
		private readonly ICode m_Code;
		private readonly BiampTesiraDevice.SubscriptionCallback m_Callback;

		public ICode Code { get { return m_Code; } }

		public BiampTesiraDevice.SubscriptionCallback Callback { get { return m_Callback; } }

		public string InstanceTag { get { return m_Code.InstanceTag; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="callback"></param>
		public CodeCallbackPair(ICode code, BiampTesiraDevice.SubscriptionCallback callback)
		{
			m_Code = code;
			m_Callback = callback;
		}

		string ISerialData.Serialize()
		{
			return m_Code.Serialize();
		}

		public bool CompareEquality(ICode other)
		{
			return m_Code.CompareEquality(other);
		}
	}
}
