using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Audio.Biamp.Controls.Dialing.Telephone;
using ICD.Connect.Audio.Biamp.Controls.Dialing.VoIP;
using ICD.Connect.Audio.Biamp.Controls.Partitioning;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Audio.Biamp.Controls.Volume;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Biamp.Controls
{
	public static class ControlsXmlUtils
	{
		/*
		XML controls are in one of 2 formats:
		
		<Control type="Volume" name="Line Volume">
			<Block>SomethingBlock</Block>
			<InstanceTag>SomethingBlock1</InstanceTag>
			<Channel type="Input">
				<Index>1</Index>
			</Channel>
		</Control>
		
		<Control type="Volume" name="Volume">
			<Block>SomethingBlock</Block>
			<InstanceTag>SomethingBlock1</InstanceTag>
		</Control>
		*/

		// Dialer controls are dependent on state controls for handling hold, do-not-disturb and privacy mute
		private static readonly string[] s_ParseOrder =
		{
			"partition",
			"state",
			"volume",
			"voip",
			"ti"
		};

		/// <summary>
		/// Instantiates device controls from the given xml document.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <returns></returns>
		public static IEnumerable<IDeviceControl> GetControlsFromXml(string xml, AttributeInterfaceFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			Dictionary<string, IDeviceControl> output = new Dictionary<string, IDeviceControl>(StringComparer.OrdinalIgnoreCase);

			foreach (string controlElement in GetControlElementsOrderedByType(xml))
			{
				IDeviceControl control = GetControlFromXml(controlElement, factory, output);
				if (control == null)
					continue;

				output.Add(control.Name, control);
			}

			return output.Values;
		}

		/// <summary>
		/// Instantiates a control from the given xml element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		[CanBeNull]
		private static IDeviceControl GetControlFromXml(string xml, AttributeInterfaceFactory factory,
		                                                IDictionary<string, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			if (cache == null)
				throw new ArgumentNullException("cache");

			string type = XmlUtils.GetAttributeAsString(xml, "type");

			switch (type.ToLower())
			{
				case "partition":
					return GetRoomCombinerWallFromXml(xml, factory);
				case "volume":
					return GetControlFromXml<BiampTesiraVolumeDeviceControl, IVolumeAttributeInterface>
						(xml, factory, (id, name, attributeInterface) =>
						               new BiampTesiraVolumeDeviceControl(id, name, attributeInterface));
				case "state":
					return GetControlFromXml<BiampTesiraStateDeviceControl, IStateAttributeInterface>
						(xml, factory, (id, name, attributeInterface) =>
						               new BiampTesiraStateDeviceControl(id, name, attributeInterface));
				case "voip":
				case "ti":
					return GetDialingControlFromXml(xml, factory, cache);

				default:
					ServiceProvider.GetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", type);
					return null;
			}
		}

		/// <summary>
		/// Instantiates a room combiner wall control from the given xml element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <returns></returns>
		private static IDeviceControl GetRoomCombinerWallFromXml(string xml, AttributeInterfaceFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			int wall = XmlUtils.ReadChildElementContentAsInt(xml, "Wall");

			return GetControlFromXml<BiampTesiraPartitionDeviceControl, RoomCombinerBlock>
						(xml, factory, (id, name, attributeInterface) =>
									   new BiampTesiraPartitionDeviceControl(id, name, attributeInterface.GetWall(wall)));
		}

		/// <summary>
		/// Instantiates a dialing control from the given xml element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		[CanBeNull]
		private static IDialingDeviceControl GetDialingControlFromXml(string xml, AttributeInterfaceFactory factory,
		                                                              IDictionary<string, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			if (cache == null)
				throw new ArgumentNullException("cache");

			string type = XmlUtils.GetAttributeAsString(xml, "type");

			string doNotDisturbName = XmlUtils.TryReadChildElementContentAsString(xml, "DoNotDisturb");
			string privacyMuteName = XmlUtils.TryReadChildElementContentAsString(xml, "PrivacyMute");
			string holdName = XmlUtils.TryReadChildElementContentAsString(xml, "Hold");

			BiampTesiraStateDeviceControl doNotDisturbControl = doNotDisturbName == null
				                                                    ? null
				                                                    : cache.GetDefault(doNotDisturbName, null) as
				                                                      BiampTesiraStateDeviceControl;
			BiampTesiraStateDeviceControl privacyMuteControl = privacyMuteName == null
				                                                   ? null
				                                                   : cache.GetDefault(privacyMuteName, null) as
				                                                     BiampTesiraStateDeviceControl;
			BiampTesiraStateDeviceControl holdControl = holdName == null
				                                            ? null
				                                            : cache.GetDefault(holdName, null) as BiampTesiraStateDeviceControl;

			switch (type.ToLower())
			{
				case "voip":
					return GetControlFromXml<VoIpDialingDeviceControl, VoIpControlStatusLine>
						(xml, factory, (id, name, attributeInterface) =>
						               new VoIpDialingDeviceControl(id, name, attributeInterface, doNotDisturbControl, privacyMuteControl));
				case "ti":
					return GetControlFromXml<TiDialingDeviceControl, TiControlStatusBlock>
						(xml, factory, (id, name, attributeInterface) =>
						               new TiDialingDeviceControl(id, name, attributeInterface, doNotDisturbControl, privacyMuteControl,
						                                          holdControl));

				default:
					ServiceProvider.GetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", type);
					return null;
			}
		}

		/// <summary>
		/// Orders the control elements based on the s_ParseOrder array.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		private static IEnumerable<string> GetControlElementsOrderedByType(string xml)
		{
			return XmlUtils.GetChildElementsAsString(xml, "Control")
						   .OrderBy(e => GetIndexFromControlElement(e));
		}

		/// <summary>
		/// Pulls the type attribute from a Control element and returns the ordered index.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		private static int GetIndexFromControlElement(string element)
		{
			string type = XmlUtils.GetAttributeAsString(element, "type");
			return s_ParseOrder.FindIndex(s => String.Equals(s, type, StringComparison.CurrentCultureIgnoreCase));
		}

		/// <summary>
		/// Shorthand for instantiating a device control from xml.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <typeparam name="TAttributeInterface"></typeparam>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <param name="constructor"></param>
		/// <returns></returns>
		private static TControl GetControlFromXml<TControl, TAttributeInterface>(string xml, AttributeInterfaceFactory factory,
		                                                                         Func<int, string, TAttributeInterface, TControl>
			                                                                         constructor)
			where TControl : IDeviceControl
			where TAttributeInterface : class, IAttributeInterface
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			if (constructor == null)
				throw new ArgumentNullException("constructor");

			int id = XmlUtils.GetAttributeAsInt(xml, "id");
			string name = XmlUtils.GetAttributeAsString(xml, "name");
			IAttributeInterface attributeInterface = GetAttributeInterfaceFromXml(xml, factory);

			TAttributeInterface concreteAttributeInterface = (TAttributeInterface)attributeInterface;
			if (concreteAttributeInterface != null)
				return constructor(id, name, concreteAttributeInterface);

			string message = string.Format("{0} is not a {1}", attributeInterface.GetType().Name,
			                               typeof(TAttributeInterface).Name);
			throw new FormatException(message);
		}

		/// <summary>
		/// Loads an AttributeInterface for the given xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="factory"></param>
		/// <returns></returns>
		private static IAttributeInterface GetAttributeInterfaceFromXml(string xml, AttributeInterfaceFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			string block = XmlUtils.ReadChildElementContentAsString(xml, "Block");
			string instanceTag = XmlUtils.ReadChildElementContentAsString(xml, "InstanceTag");

			// Get channel info showing the control wraps a block channel
			Channel channel = null;
			string channelElement;
			if (XmlUtils.TryGetChildElementAsString(xml, "Channel", out channelElement))
				channel = Channel.FromXml(channelElement);

			// Load the block
			IAttributeInterface attributeInterface = factory.LazyLoadAttributeInterface(block, instanceTag);

			// If a channel is specified, grab the child attribute
			if (channel != null)
				attributeInterface = channel.GetAttributeInterfaceChannel(attributeInterface);

			return attributeInterface;
		}
	}
}
