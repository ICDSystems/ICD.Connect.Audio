﻿using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
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
		public object[] Indices { get { return m_Indices.ToArray(); } }

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
			if (other == null)
				return false;

			T otherT = other as T;
			return otherT != null && CompareEquality(otherT);
		}

		/// <summary>
		/// Returns true if the code is equal to the given other code.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		protected virtual bool CompareEquality(T other)
		{
			if (InstanceTag != other.InstanceTag)
				return false;

			if (!Indices.SequenceEqual(other.Indices))
				return false;

			if (Value == null && other.Value == null)
				return true;
			if (Value == null)
				return false;
			if (other.Value == null)
				return false;
			return Value.CompareEquality(other.Value);
		}
	}
}
