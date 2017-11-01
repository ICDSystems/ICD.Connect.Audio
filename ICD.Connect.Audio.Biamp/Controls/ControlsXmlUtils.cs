﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
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
		
		<Control type="Volume" id="1" name="Line Volume">
			<Block>SomethingBlock</Block>
			<InstanceTag>SomethingBlock1</InstanceTag>
			<Channel type="Input">
				<Index>1</Index>
			</Channel>
		</Control>
		
		<Control type="Volume" id="2" name="Volume">
			<Block>SomethingBlock</Block>
			<InstanceTag>SomethingBlock1</InstanceTag>
		</Control>
		*/

		private static ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

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

			// First build a map of id to control elements
			Dictionary<int, string> controlElements = new Dictionary<int, string>();
			foreach (string controlElement in XmlUtils.GetChildElementsAsString(xml))
			{
				int id = XmlUtils.GetAttributeAsInt(controlElement, "id");
				controlElements.Add(id, controlElement);
			}

			// Now build the controls
			Dictionary<int, IDeviceControl> cache = new Dictionary<int, IDeviceControl>();
			foreach (int id in controlElements.Keys)
				LazyLoadControl(id, factory, controlElements, cache);

			return cache.Values.Where(v => v != null);
		}

		/// <summary>
		/// Instantiates a control from the given xml element.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="factory"></param>
		/// <param name="controlElements"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static IDeviceControl LazyLoadControl(int id, AttributeInterfaceFactory factory,
		                                              Dictionary<int, string> controlElements,
		                                              Dictionary<int, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (controlElements == null)
				throw new ArgumentNullException("controlElements");

			if (cache == null)
				throw new ArgumentNullException("cache");

			if (!controlElements.ContainsKey(id))
				throw new KeyNotFoundException(string.Format("No control element with id {0}", id));

			string xml = controlElements[id];
			string type = XmlUtils.GetAttributeAsString(xml, "type");

			switch (type.ToLower())
			{
				case "partition":
					return LazyLoadRoomCombinerWall(id, factory, controlElements, cache);
				case "volume":
					return LazyLoadControl<BiampTesiraVolumeDeviceControl, IVolumeAttributeInterface>
						(id, factory, controlElements, cache, (name, attributeInterface) =>
						                                      new BiampTesiraVolumeDeviceControl(id, name, attributeInterface));
				case "state":
					return LazyLoadControl<BiampTesiraStateDeviceControl, IStateAttributeInterface>
						(id, factory, controlElements, cache, (name, attributeInterface) =>
						                                      new BiampTesiraStateDeviceControl(id, name, attributeInterface));
				case "roomcombinerroom":
					return LazyLoadRoomCombinerRoom(id, factory, controlElements, cache);
				case "voip":
				case "ti":
					return LazyLoadDialingControl(id, factory, controlElements, cache);

				default:
					Logger.AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", type);
					return null;
			}
		}

		/// <summary>
		/// Instantiates a room combiner source control from the given xml element.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="factory"></param>
		/// <param name="controlElements"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static IDeviceControl LazyLoadRoomCombinerRoom(int id, AttributeInterfaceFactory factory,
		                                                         Dictionary<int, string> controlElements,
		                                                         Dictionary<int, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (controlElements == null)
				throw new ArgumentNullException("controlElements");

			if (cache == null)
				throw new ArgumentNullException("cache");

			if (!controlElements.ContainsKey(id))
				throw new KeyNotFoundException(string.Format("No control element with id {0}", id));

			string xml = controlElements[id];

			int room = XmlUtils.ReadChildElementContentAsInt(xml, "Room");
			int? feedbackId = XmlUtils.TryReadChildElementContentAsInt(xml, "Feedback");
			int muteSource = XmlUtils.TryReadChildElementContentAsInt(xml, "MuteSource") ?? 0;
			int unmuteSource = XmlUtils.TryReadChildElementContentAsInt(xml, "UnmuteSource") ?? 0;

			IBiampTesiraStateDeviceControl feedbackControl = feedbackId.HasValue
				                                                 ? LazyLoadControl(feedbackId.Value, factory, controlElements,
				                                                                   cache) as
				                                                   IBiampTesiraStateDeviceControl
				                                                 : null;

			return LazyLoadControl<RoomCombinerRoomStateControl, RoomCombinerBlock>
				(id, factory, controlElements, cache, (name, attributeInterface) =>
				                                      new RoomCombinerRoomStateControl(id, name, muteSource, unmuteSource,
				                                                                       attributeInterface.GetRoom(room),
				                                                                       feedbackControl));
		}

		/// <summary>
		/// Instantiates a room combiner wall control from the given xml element.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="factory"></param>
		/// <param name="controlElements"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static IDeviceControl LazyLoadRoomCombinerWall(int id, AttributeInterfaceFactory factory,
		                                                       Dictionary<int, string> controlElements,
		                                                       Dictionary<int, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (controlElements == null)
				throw new ArgumentNullException("controlElements");

			if (cache == null)
				throw new ArgumentNullException("cache");

			if (!controlElements.ContainsKey(id))
				throw new KeyNotFoundException(string.Format("No control element with id {0}", id));

			string xml = controlElements[id];
			int wall = XmlUtils.ReadChildElementContentAsInt(xml, "Wall");

			return LazyLoadControl<BiampTesiraPartitionDeviceControl, RoomCombinerBlock>
				(id, factory, controlElements, cache, (name, attributeInterface) =>
				                                      new BiampTesiraPartitionDeviceControl(id, name,
				                                                                            attributeInterface.GetWall(wall)));
		}

		/// <summary>
		/// Instantiates a dialing control from the given xml element.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="factory"></param>
		/// <param name="controlElements"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		[CanBeNull]
		private static IDialingDeviceControl LazyLoadDialingControl(int id, AttributeInterfaceFactory factory,
		                                                            Dictionary<int, string> controlElements,
		                                                            Dictionary<int, IDeviceControl> cache)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (controlElements == null)
				throw new ArgumentNullException("controlElements");

			if (cache == null)
				throw new ArgumentNullException("cache");

			if (!controlElements.ContainsKey(id))
				throw new KeyNotFoundException(string.Format("No control element with id {0}", id));

			string xml = controlElements[id];
			string type = XmlUtils.GetAttributeAsString(xml, "type");

			int? doNotDisturbId = XmlUtils.TryReadChildElementContentAsInt(xml, "DoNotDisturb");
			int? privacyMuteId = XmlUtils.TryReadChildElementContentAsInt(xml, "PrivacyMute");
			int? holdId = XmlUtils.TryReadChildElementContentAsInt(xml, "Hold");

			IBiampTesiraStateDeviceControl doNotDisturbControl = doNotDisturbId.HasValue
				                                                    ? LazyLoadControl(doNotDisturbId.Value, factory, controlElements,
																					  cache) as IBiampTesiraStateDeviceControl
				                                                    : null;

			IBiampTesiraStateDeviceControl privacyMuteControl = privacyMuteId.HasValue
				                                                   ? LazyLoadControl(privacyMuteId.Value, factory, controlElements,
																					 cache) as IBiampTesiraStateDeviceControl
				                                                   : null;

			IBiampTesiraStateDeviceControl holdControl = holdId.HasValue
				                                             ? LazyLoadControl(holdId.Value, factory, controlElements, cache) as
				                                               IBiampTesiraStateDeviceControl
				                                             : null;

			switch (type.ToLower())
			{
				case "voip":
					return LazyLoadControl<VoIpDialingDeviceControl, VoIpControlStatusLine>
						(id, factory, controlElements, cache, (name, attributeInterface) =>
						                                      new VoIpDialingDeviceControl(id, name, attributeInterface,
						                                                                   doNotDisturbControl, privacyMuteControl));
				case "ti":
					return LazyLoadControl<TiDialingDeviceControl, TiControlStatusBlock>
						(id, factory, controlElements, cache, (name, attributeInterface) =>
						                                      new TiDialingDeviceControl(id, name, attributeInterface, doNotDisturbControl,
						                                                                 privacyMuteControl, holdControl));

				default:
					Logger.AddEntry(eSeverity.Error, "Unable to create control for unknown type \"{0}\"", type);
					return null;
			}
		}

		/// <summary>
		/// Shorthand for instantiating a device control from xml.
		/// </summary>
		/// <typeparam name="TControl"></typeparam>
		/// <typeparam name="TAttributeInterface"></typeparam>
		/// <param name="id"></param>
		/// <param name="factory"></param>
		/// <param name="controlElements"></param>
		/// <param name="cache"></param>
		/// <param name="constructor"></param>
		/// <returns></returns>
		private static TControl LazyLoadControl<TControl, TAttributeInterface>(int id, AttributeInterfaceFactory factory,
		                                                                       Dictionary<int, string> controlElements,
		                                                                       Dictionary<int, IDeviceControl> cache,
		                                                                       Func<string, TAttributeInterface, TControl>
			                                                                       constructor)
			where TControl : class, IDeviceControl
			where TAttributeInterface : class, IAttributeInterface
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (controlElements == null)
				throw new ArgumentNullException("controlElements");

			if (cache == null)
				throw new ArgumentNullException("cache");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			if (!controlElements.ContainsKey(id))
				throw new KeyNotFoundException(string.Format("No control element with id {0}", id));

			if (!cache.ContainsKey(id))
			{
				string xml = controlElements[id];
				string name = XmlUtils.GetAttributeAsString(xml, "name");
				IAttributeInterface attributeInterface = GetAttributeInterfaceFromXml(xml, factory);

				TAttributeInterface concreteAttributeInterface;

				try
				{
					concreteAttributeInterface = (TAttributeInterface)attributeInterface;
				}
				catch (InvalidCastException e)
				{
					string castMessage = string.Format("{0} is not of type {1}", attributeInterface.GetType().Name,
					                                   typeof(TAttributeInterface).Name);
					throw new InvalidCastException(castMessage, e);
				}

				TControl control = constructor(name, concreteAttributeInterface);
				cache.Add(id, control);
			}

			return cache[id] as TControl;
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
