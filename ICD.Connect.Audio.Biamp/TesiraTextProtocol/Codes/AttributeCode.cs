using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes
{
	/// <summary>
	/// Builds an attribute code for communication with the Tesira device:
	/// 
	///		InstanceTag
	///		Command
	///		Attribute
	///		Index[] (optional)
	///		Value (optional)
	///		Line Feed
	/// 
	/// E.g:
	/// 
	///		MatMix_1 set crosspointLevel 4 6 -4 LF
	/// </summary>
	public sealed class AttributeCode : AbstractCode<AttributeCode>
	{
		public enum eCommand
		{
			Get,
			Set,
			Increment,
			Decrement,
			Toggle,
			Subscribe,
			Unsubscribe
		}

		private readonly eCommand m_Command;
		private readonly string m_Attribute;

		#region Properties

		[PublicAPI]
		public eCommand Command { get { return m_Command; } }

		[PublicAPI]
		public string Attribute { get { return m_Attribute; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="command"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		private AttributeCode(string instanceTag, eCommand command, string attribute, IValue value, object[] indices)
			: base(instanceTag, value, indices)
		{
			m_Command = command;
			m_Attribute = attribute;
		}

		/// <summary>
		/// Builds an attribute Get command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Get(string instanceTag, string attribute, params int[] indices)
		{
			return new AttributeCode(instanceTag, eCommand.Get, attribute, null, indices.Cast<object>().ToArray());
		}

		/// <summary>
		/// Builds an attribute Set command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Set(string instanceTag, string attribute, IValue value, params int[] indices)
		{
			return new AttributeCode(instanceTag, eCommand.Set, attribute, value, indices.Cast<object>().ToArray());
		}

		/// <summary>
		/// Builds and attribute increment command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Increment(string instanceTag, string attribute, IValue value, params int[] indices)
		{
			return new AttributeCode(instanceTag, eCommand.Increment, attribute, value, indices.Cast<object>().ToArray());
		}

		/// <summary>
		/// Builds and attribute decrement command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Decrement(string instanceTag, string attribute, IValue value, params int[] indices)
		{
			return new AttributeCode(instanceTag, eCommand.Decrement, attribute, value, indices.Cast<object>().ToArray());
		}

		/// <summary>
		/// Builds and attribute toggle command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Toggle(string instanceTag, string attribute, params int[] indices)
		{
			return new AttributeCode(instanceTag, eCommand.Toggle, attribute, null, indices.Cast<object>().ToArray());
		}

		/// <summary>
		/// Builds a subscription command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="key"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Subscribe(string instanceTag, string attribute, string key, params int[] indices)
		{
			object[] finalIndices = indices.Cast<object>()
										   .Append(key)
										   .ToArray();

			return new AttributeCode(instanceTag, eCommand.Subscribe, attribute, null, finalIndices);
		}

		/// <summary>
		/// Builds an unsubscribe command.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="key"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static AttributeCode Unsubscribe(string instanceTag, string attribute, string key, params int[] indices)
		{
			object[] finalIndices = indices.Cast<object>()
			                               .Append(key)
			                               .ToArray();

			return new AttributeCode(instanceTag, eCommand.Unsubscribe, attribute, null, finalIndices);
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

			// Command
			builder.Append(' ');
// ReSharper disable once ImpureMethodCallOnReadonlyValueField
			builder.Append(m_Command.ToString().ToLower());

			// Attribute
			builder.Append(' ');
			builder.Append(m_Attribute);

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
		public override bool CompareEquality(AttributeCode other)
		{
			if (other == null)
				return false;

			if (m_Command != other.m_Command)
				return false;

			return m_Attribute == other.m_Attribute && base.CompareEquality(other);
		}
	}
}
