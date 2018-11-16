using System;
using System.Linq;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol
{
	/// <summary>
	/// Simple pairing of a subscription callback and an attribute code.
	/// </summary>
	public sealed class SubscriptionCallbackInfo : IEquatable<SubscriptionCallbackInfo>
	{
		private readonly BiampTesiraDevice.SubscriptionCallback m_Callback;
		private readonly AttributeCode m_Code;

		/// <summary>
		/// Gets the callback for the subscription.
		/// </summary>
		public BiampTesiraDevice.SubscriptionCallback Callback { get { return m_Callback; } }

		/// <summary>
		/// Gets the attribute code for generating the subscription serial.
		/// </summary>
		public AttributeCode Code { get { return m_Code; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="code"></param>
		public SubscriptionCallbackInfo(BiampTesiraDevice.SubscriptionCallback callback,
		                                AttributeCode code)
		{
			m_Callback = callback;
			m_Code = code;
		}

		/// <summary>
		/// Generates a subscription key for the given attribute so we can match system feedback to a callback.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static string GenerateSubscriptionKey(string instanceTag, string attribute, params int[] indices)
		{
			string indicesString = string.Join("-", indices.Select(i => i.ToString()).ToArray());
			return string.Format("{0}-{1}-{2}", instanceTag, attribute, indicesString);
		}

		public bool Equals(SubscriptionCallbackInfo other)
		{
			return other != null &&
			       other.m_Callback == m_Callback &&
			       other.m_Code == m_Code;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + m_Callback.GetHashCode();
				hash = hash * 23 + m_Code.GetHashCode();
				return hash;
			}
		}
	}
}
