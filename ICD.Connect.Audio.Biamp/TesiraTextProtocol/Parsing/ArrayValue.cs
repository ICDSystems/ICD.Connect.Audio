using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	public sealed class ArrayValue : AbstractValue, ICollection<AbstractValue>
	{
		private readonly List<AbstractValue> m_Values;

		/// <summary>
		/// Gets the child value at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public AbstractValue this[int index]
		{
			get
			{
				if (index >= 0 && index < m_Values.Count)
					return m_Values[index];

				string message = string.Format("{0} has no item at index {1}", GetType().Name, index);
				throw new KeyNotFoundException(message);
			}
		}

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public ArrayValue()
			: this(Enumerable.Empty<AbstractValue>())
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="values"></param>
		public ArrayValue(IEnumerable<AbstractValue> values)
		{
			m_Values = new List<AbstractValue>(values);
		}

		/// <summary>
		/// Deserializes the serialized data to an ArrayValue.
		/// </summary>
		/// <param name="serialized"></param>
		/// <returns></returns>
		public static ArrayValue Deserialize(string serialized)
		{
			IEnumerable<AbstractValue> values = TtpUtils.GetArrayValues(serialized);
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

		#endregion

		#region IEnumerable

		public IEnumerator<AbstractValue> GetEnumerator()
		{
			return m_Values.ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICollections Methods

		public void Add(AbstractValue item)
		{
			m_Values.Add(item);
		}

		public void Clear()
		{
			m_Values.Clear();
		}

		public bool Contains(AbstractValue item)
		{
			return m_Values.Contains(item);
		}

		public void CopyTo(AbstractValue[] array, int arrayIndex)
		{
			m_Values.CopyTo(array, arrayIndex);
		}

		public bool Remove(AbstractValue item)
		{
			return m_Values.Remove(item);
		}

		public int Count { get { return m_Values.Count; } }
		public bool IsReadOnly { get { return false; } }

		#endregion
	}
}
