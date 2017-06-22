using System.Linq;
using System.Text;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
{
	public sealed class ServiceCode : AbstractCode
	{
		private readonly string m_Service;

		public string Service { get { return m_Service; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="service"></param>
		public ServiceCode(string instanceTag, string service)
			: this(instanceTag, service, new Value(null))
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="service"></param>
		/// <param name="value"></param>
		public ServiceCode(string instanceTag, string service, AbstractValue value)
			: this(instanceTag, service, value, new object[0])
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="service"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		public ServiceCode(string instanceTag, string service, AbstractValue value, object[] indices)
			: base(instanceTag, value, indices)
		{
			m_Service = service;
		}

		#endregion

		/// <summary>
		/// Returns the code as a TTP serial command.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			// Instance
			if (InstanceTag.Any(char.IsWhiteSpace))
				builder.Append(string.Format("\"{0}\"", InstanceTag));
			else
				builder.Append(InstanceTag);

			// Service
			builder.Append(' ');
			builder.Append(Service);

			// Indices
			if (Indices.Length > 0)
			{
				string indices = string.Join(" ", Indices.Select(i => i.ToString()).ToArray());
				builder.Append(' ');
				builder.Append(indices);
			}

			// Value
			if (Value != null)
			{
				builder.Append(' ');
				builder.Append(Value.Serialize());
			}

			builder.Append(TtpUtils.LF);

			return builder.ToString();
		}
	}
}
