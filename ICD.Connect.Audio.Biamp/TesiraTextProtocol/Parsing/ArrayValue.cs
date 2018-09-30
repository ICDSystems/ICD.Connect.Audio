using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing
{
	/// <summary>
	/// An array value is a collection of AbstractValues.
	/// 
	/// Serialized it may look like:
	/// 
	/// +OK "list":["123" "AudioMeter1" "AudioMeter2" "AudioMeter3" "DEVICE"
	///				"Input1" "Mixer1" "Mute1" "Level1" "Output1"]
	/// </summary>
	public sealed class ArrayValue : AbstractValue<ArrayValue>, ICollection<IValue>
	{
		private readonly List<IValue> m_Values;

		/// <summary>
		/// Gets the child value at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IValue this[int index]
		{
			get
			{
				if (index >= 0 && index < m_Values.Count)
					return m_Values[index];

				string message = string.Format("{0} has no item at index {1}", GetType().Name, index);
				throw new ArgumentOutOfRangeException("index", message);
			}
		}

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public ArrayValue()
			: this(Enumerable.Empty<IValue>())
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="values"></param>
		public ArrayValue(IEnumerable<IValue> values)
		{
			m_Values = new List<IValue>(values);
		}

		/// <summary>
		/// Deserializes the serialized data to an ArrayValue.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static ArrayValue Deserialize(string serialized)
		{
			IEnumerable<IValue> values = TtpUtils.GetArrayValues(serialized);
			return new ArrayValue(values);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Serializes the value to a string in TTP format.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append('[');

			string contents = string.Join(" ", m_Values.Select(v => v.Serialize()).ToArray());
			builder.Append(contents);

			builder.Append(']');

			return builder.ToString();
		}

		/// <summary>
		/// Compares this values equality with the given other value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		protected override bool CompareEquality(ArrayValue other)
		{
			return other.SequenceEqual(other, (a, b) => a.CompareEquality(b));
		}

		#endregion

		#region IEnumerable

		public IEnumerator<IValue> GetEnumerator()
		{
			return m_Values.ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICollections Methods

		public void Add(IValue item)
		{
			m_Values.Add(item);
		}

		public void Clear()
		{
			m_Values.Clear();
		}

		public bool Contains(IValue item)
		{
			return m_Values.Contains(item);
		}

		public void CopyTo(IValue[] array, int arrayIndex)
		{
			m_Values.CopyTo(array, arrayIndex);
		}

		public bool Remove(IValue item)
		{
			return m_Values.Remove(item);
		}

		public int Count { get { return m_Values.Count; } }
		public bool IsReadOnly { get { return false; } }

		#endregion
	}
}
