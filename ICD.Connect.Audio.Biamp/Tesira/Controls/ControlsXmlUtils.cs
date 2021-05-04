using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Audio.Biamp.Tesira.Controls.Dialing.Telephone;
using ICD.Connect.Audio.Biamp.Tesira.Controls.Dialing.VoIP;
using ICD.Connect.Audio.Biamp.Tesira.Controls.Partitioning;
using ICD.Connect.Audio.Biamp.Tesira.Controls.State;
using ICD.Connect.Audio.Biamp.Tesira.Controls.Volume;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Utils;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls
{
	public static class ControlsXmlUtils
	{
		/*
		XML controls are in one of 2 formats:
		
		<Control type="Volume" id="1" uuid="0D79ED58-BCC3-4d2d-B206-68938C33720E" name="Line Volume">
			<Block>SomethingBlock</Block>
			<InstanceTag>SomethingBlock1</InstanceTag>
			<Channel type="Input">
				<Index>1</Index>
			</Channel>
		</Control>
		
		<Control type="Volume" id="2" uuid="C6B7650C-0814-4f26-8BAE-507C1CD42DA3" name="Volume">
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
		[CanBeNull]
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

			try
			{
				switch (type.ToLower())
				{
					case "partition":
						return LazyLoadRoomCombinerWall(id, factory, controlElements, cache);
					case "volume":
						return LazyLoadControl<BiampTesiraVolumeDeviceControl, IVolumeAttributeInterface>
							(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
								 new BiampTesiraVolumeDeviceControl(id, uuid, name, attributeInterface));
					case "privacy":
						return LazyLoadControl<BiampTesiraPrivacyMuteDeviceControl, IStateAttributeInterface>
							(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
								 new BiampTesiraPrivacyMuteDeviceControl(id, uuid, name, attributeInterface));
					case "state":
						return LazyLoadControl<BiampTesiraStateDeviceControl, IStateAttributeInterface>
							(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
								 new BiampTesiraStateDeviceControl(id, uuid, name, attributeInterface));
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
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "Failed to load control {0}", id);
			}

			return null;
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
				(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
				                                      new RoomCombinerRoomStateControl(id, uuid, name, muteSource, unmuteSource,
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
				(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
				                                      new BiampTesiraPartitionDeviceControl(id, uuid, name,
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
		private static IConferenceDeviceControl LazyLoadDialingControl(int id, AttributeInterfaceFactory factory,
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
			string callInNumber = XmlUtils.TryReadChildElementContentAsString(xml, "CallInNumber");

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
					DialContext voipInfo =
						new DialContext
						{
							Protocol = eDialProtocol.Sip,
							CallType = eCallType.Audio,
							DialString = callInNumber
						};

					return LazyLoadControl<VoIpConferenceDeviceControl, VoIpControlStatusLine>
						(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
						                                      new VoIpConferenceDeviceControl(id, uuid, name, attributeInterface,
						                                                                      privacyMuteControl, voipInfo));
				case "ti":
					DialContext tiInfo =
						new DialContext
						{
							Protocol = eDialProtocol.Pstn,
							CallType = eCallType.Audio,
							DialString = callInNumber
						};

					return LazyLoadControl<TiConferenceDeviceControl, TiControlStatusBlock>
						(id, factory, controlElements, cache, (uuid, name, attributeInterface) =>
						                                      new TiConferenceDeviceControl(id, uuid, name, attributeInterface,
						                                                                    doNotDisturbControl,
						                                                                    privacyMuteControl, holdControl,
																							tiInfo));

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
		                                                                       Func<Guid, string, TAttributeInterface, TControl>
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
				Guid uuid;

				try
				{
					uuid = XmlUtils.GetAttributeAsGuid(xml, "uuid");
				}
				catch (Exception)
				{
					uuid = DeviceControlUtils.GenerateUuid(factory.Device, id);
				}

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

				TControl control = constructor(uuid, name, concreteAttributeInterface);
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
			try
			{
				string channelElement;
				if (XmlUtils.TryGetChildElementAsString(xml, "Channel", out channelElement))
					channel = Channel.FromXml(channelElement);
			}
			// Hack - When no index is specified treat it as null channel
			catch (FormatException)
			{
			}

			// Load the block
			IAttributeInterface attributeInterface = factory.LazyLoadAttributeInterface(block, instanceTag);

			// If a channel is specified, grab the child attribute
			if (channel != null)
				attributeInterface = channel.GetAttributeInterfaceChannel(attributeInterface);

			return attributeInterface;
		}
	}
}
