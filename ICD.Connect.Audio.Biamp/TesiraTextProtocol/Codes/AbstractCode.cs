using System.Linq;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
{
	public abstract class AbstractCode : ICode
	{
		private readonly string m_InstanceTag;
		private readonly AbstractValue m_Value;
		private readonly object[] m_Indices;

		public string InstanceTag { get { return m_InstanceTag; } }
		public object[] Indices { get { return m_Indices.ToArray(); } }
		public AbstractValue Value { get { return m_Value; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		protected AbstractCode(string instanceTag, AbstractValue value, object[] indices)
		{
			m_InstanceTag = instanceTag;
			m_Indices = indices;
			m_Value = value;
		}

		/// <summary>
		/// Returns the code as a TTP serial command.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();
	}
}
