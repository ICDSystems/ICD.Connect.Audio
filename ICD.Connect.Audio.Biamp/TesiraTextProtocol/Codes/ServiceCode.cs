using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
{
	public sealed class ServiceCode : AbstractCode<ServiceCode>
	{
		private readonly string m_Service;

		[PublicAPI]
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
		public ServiceCode(string instanceTag, string service, IValue value)
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
		public ServiceCode(string instanceTag, string service, IValue value, object[] indices)
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
			string instanceTag = InstanceTag.Any(char.IsWhiteSpace)
				                     ? string.Format("\"{0}\"", InstanceTag)
				                     : InstanceTag;
			builder.Append(instanceTag);

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

		/// <summary>
		/// Returns true if the code is equal to the given other code.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool CompareEquality(ServiceCode other)
		{
			if (other == null)
				return false;

			return m_Service == other.m_Service && base.CompareEquality(other);
		}
	}
}
