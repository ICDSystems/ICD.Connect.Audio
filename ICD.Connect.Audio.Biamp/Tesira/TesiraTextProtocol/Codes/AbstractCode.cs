using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes
{
	public abstract class AbstractCode<T> : ICode
		where T : AbstractCode<T>
	{
		private readonly string m_InstanceTag;
		private readonly IValue m_Value;
		private readonly object[] m_Indices;

		#region Properties

		public string InstanceTag { get { return m_InstanceTag; } }

		[NotNull]
		public object[] Indices { get { return m_Indices.ToArray(m_Indices.Length); } }

		[CanBeNull]
		public IValue Value { get { return m_Value; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		protected AbstractCode(string instanceTag, IValue value, object[] indices)
		{
			if (indices == null)
				throw new ArgumentNullException("indices");

			m_InstanceTag = instanceTag;
			m_Indices = indices;
			m_Value = value;
		}

		/// <summary>
		/// Returns the code as a TTP serial command.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();

		/// <summary>
		/// Returns true if the code is equal to the given other code.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool ICode.CompareEquality(ICode other)
		{
			return CompareEquality(other as T);
		}

		/// <summary>
		/// Returns true if the code is equal to the given other code.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual bool CompareEquality(T other)
		{
			if (other == null)
				return false;

			if (m_InstanceTag != other.m_InstanceTag)
				return false;

			if (m_Value == null)
			{
				if (other.m_Value != null)
					return false;
			}
			else
			{
				if (!m_Value.CompareEquality(other.m_Value))
					return false;
			}

			return m_Indices.SequenceEqual(other.m_Indices);
		}
	}
}
