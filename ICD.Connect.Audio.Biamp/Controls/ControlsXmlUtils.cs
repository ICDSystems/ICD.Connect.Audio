using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.Controls.Dialing.Telephone;
using ICD.Connect.Audio.Biamp.Controls.Dialing.VoIP;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Audio.Biamp.Controls.Volume;
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
			List<IDeviceControl> output = new List<IDeviceControl>();

			foreach (string controlElement in GetControlElementsOrderedByType(xml))
			{
				string type = XmlUtils.GetAttributeAsString(controlElement, "type");
				IDeviceControl control;

				switch (type.ToLower())
				{
					case "volume":
						control = GetControlFromXml<BiampTesiraVolumeDeviceControl, IVolumeAttributeInterface>
							(controlElement, factory, (id, name, attributeInterface) =>
													  new BiampTesiraVolumeDeviceControl(id, name, attributeInterface));
						break;

					case "state":
						control = GetControlFromXml<BiampTesiraStateDeviceControl, IStateAttributeInterface>
							(controlElement, factory, (id, name, attributeInterface) =>
													  new BiampTesiraStateDeviceControl(id, name, attributeInterface));
						break;

					case "voip":
					case "ti":
						string doNotDisturbName = XmlUtils.TryReadChildElementContentAsString(controlElement, "DoNotDisturb");
						string privacyMuteName = XmlUtils.TryReadChildElementContentAsString(controlElement, "PrivacyMute");
						string holdName = XmlUtils.TryReadChildElementContentAsString(controlElement, "Hold");

						BiampTesiraStateDeviceControl doNotDisturbControl =
							output.FirstOrDefault(c => c.Name == doNotDisturbName) as BiampTesiraStateDeviceControl;
						BiampTesiraStateDeviceControl privacyMuteControl =
							output.FirstOrDefault(c => c.Name == privacyMuteName) as BiampTesiraStateDeviceControl;
						BiampTesiraStateDeviceControl holdControl =
							output.FirstOrDefault(c => c.Name == holdName) as BiampTesiraStateDeviceControl;

						if (type.ToLower() == "voip")
						{
							control = GetControlFromXml<VoIpDialingDeviceControl, VoIpControlStatusLine>
								(controlElement, factory, (id, name, attributeInterface) =>
											   new VoIpDialingDeviceControl(id, name, attributeInterface, doNotDisturbControl, privacyMuteControl));
						}
						else
						{
							control = GetControlFromXml<TiDialingDeviceControl, TiControlStatusBlock>
								(controlElement, factory, (id, name, attributeInterface) =>
											   new TiDialingDeviceControl(id, name, attributeInterface, doNotDisturbControl, privacyMuteControl, holdControl));
						}

						break;

					default:
						IcdErrorLog.Error("Unable to create control for unknown type \"{0}\"", type);
						continue;
				}

				output.Add(control);
			}

			return output;
		}

		/// <summary>
		/// Orders the control elements based on the s_ParseOrder array.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		private static IEnumerable<string> GetControlElementsOrderedByType(string xml)
		{
			return XmlUtils.GetChildElementsAsString(xml, "Control")
			               .OrderBy(e =>
			                        {
				                        string type = XmlUtils.GetAttributeAsString(e, "type");
				                        return
					                        s_ParseOrder.FindIndex(s =>
					                                               String.Equals(s, type, StringComparison.CurrentCultureIgnoreCase));
			                        });
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
