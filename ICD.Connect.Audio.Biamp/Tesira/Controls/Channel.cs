using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls
{
	public enum eChannelType
	{
		None,
		Input,
		Output,
		Crosspoint
	}

	/// <summary>
	/// Simply parses some info from XML for looking up channels.
	/// </summary>
	public sealed class Channel
	{
		private readonly eChannelType m_ChannelType;
		private readonly int[] m_Indices;

		/// <summary>
		/// Gets the channel type.
		/// </summary>
		[PublicAPI]
		public eChannelType ChannelType { get { return m_ChannelType; } }

		/// <summary>
		/// Gets the indices for the channel. This will be a single item for an input/output, but 2 for a crosspoint.
		/// </summary>
		[PublicAPI]
		public IEnumerable<int> Indices { get { return m_Indices.ToArray(); } } 

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="channelType"></param>
		/// <param name="indices"></param>
		public Channel(eChannelType channelType, IEnumerable<int> indices)
		{
			m_ChannelType = channelType;
			m_Indices = indices.ToArray();
		}

		/// <summary>
		/// Gets the string representation for this Channel.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("ChannelType", ChannelType);
			builder.AppendProperty("Indices", StringUtils.ArrayFormat(Indices));

			return builder.ToString();
		}

		/// <summary>
		/// Deserializes a channel from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static Channel FromXml(string xml)
		{
			string channelTypeString = XmlUtils.GetAttributeAsString(xml, "type");
			eChannelType channelType = string.IsNullOrEmpty(channelTypeString)
				                           ? eChannelType.None
				                           : EnumUtils.Parse<eChannelType>(channelTypeString, true);

			int[] indices = XmlUtils.GetChildElementsAsString(xml, "Index")
			                        .Select(e => XmlUtils.ReadElementContentAsInt(e))
			                        .ToArray();

			return new Channel(channelType, indices);
		}

		/// <summary>
		/// Gets the channel attribute interface for the given attribute interface.
		/// </summary>
		/// <param name="attributeInterface"></param>
		/// <returns></returns>
		public IAttributeInterface GetAttributeInterfaceChannel(IAttributeInterface attributeInterface)
		{
			return attributeInterface.GetAttributeInterface(m_ChannelType, m_Indices);
		}
	}
}
