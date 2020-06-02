using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.ControlBlocks.Dialer;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.CrossoverBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.DelayBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.DynamicBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.EqualizerBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.FilterBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.GeneratorBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.Aec;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.VoIp;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.LogicBlocks.LogicState;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MeterBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GainSharingAutoMixer;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.GatingAutoMixer;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.StandardMixer;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.Router;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.RouterBlocks.SourceSelector;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.Services;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces
{
	/// <summary>
	/// Caches AttributeInterfaces by their instance tags for lazy loading.
	/// </summary>
	public sealed class AttributeInterfaceFactory : IConsoleNode, IDisposable
	{
		private readonly Dictionary<string, AbstractAttributeInterface> m_Cache;
		private readonly SafeCriticalSection m_CacheSection;

		private readonly BiampTesiraDevice m_Device;

		private static readonly Dictionary<string, Type> s_ServiceTags =
			new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
		{
			{DeviceService.INSTANCE_TAG, typeof(DeviceService)},
			{SessionService.INSTANCE_TAG, typeof(SessionService)}
		};

		private static readonly Type[] s_AttributeInterfaceTypes =
		{
			typeof(DialerBlock),
			typeof(CommandStringBlock),
			typeof(InvertControlBlock),
			typeof(LevelControlBlock),
			typeof(LevelControlChannel),
			typeof(MuteControlBlock),
			typeof(MuteControlChannel),
			typeof(PresetControlBlock),
			typeof(CrossoverBlock),
			typeof(AudioDelayBlock),
			typeof(AgcBlock),
			typeof(CompressorBlock),
			typeof(DuckerBlock),
			typeof(LevelerBlock),
			typeof(NoiseGateBlock),
			typeof(PeakLimiterBlock),
			typeof(FeedbackSuppressorBlock),
			typeof(GraphicEqualizerBlock),
			typeof(ParametricEqualizerBlock),
			typeof(AllPassFilterBlock),
			typeof(PassFilterBlock),
			typeof(ShelfFilterBlock),
			typeof(UberFilterBlock),
			typeof(NoiseGeneratorBlock),
			typeof(ToneGeneratorBlock),
			typeof(AecInputBlock),
			typeof(AecProcessingBlock),
			typeof(TiControlStatusBlock),
			typeof(TiReceiveBlock),
			typeof(TiTransmitBlock),
			typeof(VoIpControlStatusBlock),
			typeof(VoIpReceiveBlock),
			typeof(VoIpTransmitBlock),
			typeof(AncInputBlock),
			typeof(AncProcessingBlock),
			typeof(AudioInputBlock),
			typeof(AudioOutputBlock),
			typeof(CobraNetInputBlock),
			typeof(CobraNetOutputBlock),
			typeof(DanteInputBlock),
			typeof(DanteOutputBlock),
			typeof(DtmfDecodeBlock),
			typeof(TcCallStateCommands),
			typeof(UsbInputBlock),
			typeof(UsbOutputBlock),
			typeof(LogicStateBlock),
			typeof(ControlVoltageBlock),
			typeof(FlipFlopBlock),
			typeof(LogicDelayBlock),
			typeof(LogicInputBlock),
			typeof(LogicMeterBlock),
			typeof(LogicOutputBlock),
			typeof(PeakOrRmsMeterBlock),
			typeof(SignalPresentMeterBlock),
			typeof(GainSharingAutoMixerBlock),
			typeof(GatingAutoMixerBlock),
			typeof(StandardMixerBlock),
			typeof(AutoMixerCombinerBlock),
			typeof(MatrixMixerBlock),
			typeof(RoomCombinerBlock),
			typeof(RouterBlock),
			typeof(SourceSelectorBlock)
		};

		private static readonly Dictionary<string, Type> s_NameToAttributeInterface; 

		#region Properties

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().GetNameWithoutGenericArity(); } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets the parent device for this factory.
		/// </summary>
		public BiampTesiraDevice Device { get { return m_Device; } }

		#endregion

		/// <summary>
		/// Static constructor.
		/// </summary>
		static AttributeInterfaceFactory()
		{
			s_NameToAttributeInterface = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
			foreach (Type type in s_AttributeInterfaceTypes)
				s_NameToAttributeInterface[type.Name] = type;
		}

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		public AttributeInterfaceFactory(BiampTesiraDevice device)
		{
			m_Cache = new Dictionary<string, AbstractAttributeInterface>();
			m_CacheSection = new SafeCriticalSection();

			m_Device = device;
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AttributeInterfaceFactory()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			ClearCache();
		}

		/// <summary>
		/// Clears loaded attribute interfaces.
		/// </summary>
		[PublicAPI]
		public void ClearCache()
		{
			m_CacheSection.Enter();

			try
			{
				foreach (AbstractAttributeInterface attributeInterface in m_Cache.Values)
					attributeInterface.Dispose();
				m_Cache.Clear();
			}
			finally
			{
				m_CacheSection.Leave();
			}
		}

		/// <summary>
		/// Returns the interface with the given instance tag.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		[PublicAPI]
		public AbstractAttributeInterface GetAttributeInterface(string instanceTag)
		{
			m_CacheSection.Enter();

			try
			{
				if (m_Cache.ContainsKey(instanceTag))
					return m_Cache[instanceTag];
			}
			finally
			{
				m_CacheSection.Leave();
			}

			string message = string.Format("No AttributeInterface with tag {0}", instanceTag);
			throw new KeyNotFoundException(message);
		}

		/// <summary>
		/// Returns the cached interfaces.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<AbstractAttributeInterface> GetAttributeInterfaces()
		{
			return m_CacheSection.Execute(() => m_Cache.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the interface with the given instance tag.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		[PublicAPI]
		public T GetAttributeInterface<T>(string instanceTag)
			where T : AbstractAttributeInterface
		{
			AbstractAttributeInterface output = GetAttributeInterface(instanceTag);
			if (output is T)
				return output as T;

			string message = string.Format("{0} is of type {1}, not {2}", instanceTag, output.GetType().Name, typeof(T).Name);
			throw new InvalidCastException(message);
		}

		/// <summary>
		/// Gets the service of the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[PublicAPI]
		public T GetService<T>()
			where T : AbstractService
		{
			string instanceTag = GetInstanceTagForService<T>();
			return GetAttributeInterface(instanceTag) as T;
		}

		/// <summary>
		/// Gets the existing AttributeInterface with the given instance tag, or instantiates a new one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		[PublicAPI]
		public T LazyLoadAttributeInterface<T>(string instanceTag)
			where T : AbstractAttributeInterface
		{
			return LazyLoadAttributeInterface(typeof(T).Name, instanceTag) as T;
		}

	    [PublicAPI]
	    public bool TryGetAttributeInterface<T>(out T attributeInterface)
	        where T : AbstractAttributeInterface
	    {
            attributeInterface = null;

	        foreach (var kvp in m_Cache)
	        {
	            if (!(kvp.Value is T))
	                continue;
	            attributeInterface = kvp.Value as T;
	            return true;
	        }
	        
	        return false;
	    }

	    /// <summary>
		/// Gets the existing AttributeInterface of the given type with the given instance tag, or instantiates a new one.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		[PublicAPI]
		public AbstractAttributeInterface LazyLoadAttributeInterface(string type, string instanceTag)
		{
			m_CacheSection.Enter();

			try
			{
				if (!m_Cache.ContainsKey(instanceTag))
					m_Cache[instanceTag] = InstantiateAttributeInterface(type, instanceTag);
				return GetAttributeInterface(instanceTag);
			}
			finally
			{
				m_CacheSection.Leave();
			}
		}

		/// <summary>
		/// Gets the existing service, or instantiates a new one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[PublicAPI]
		public T LazyLoadService<T>()
			where T : AbstractService
		{
			string instanceTag = GetInstanceTagForService<T>();
			return LazyLoadService(instanceTag) as T;
		}

		/// <summary>
		/// Gets the existing service, or instantiates a new one.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		[PublicAPI]
		public AbstractService LazyLoadService(string instanceTag)
		{
			m_CacheSection.Enter();

			try
			{
				if (!m_Cache.ContainsKey(instanceTag))
					m_Cache[instanceTag] = InstantiateService(instanceTag);
				return m_Cache[instanceTag] as AbstractService;
			}
			finally
			{
				m_CacheSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Uses reflection to instantiate the attribute interface with the given type and instance tag.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		private AbstractAttributeInterface InstantiateAttributeInterface(string type, string instanceTag)
		{
			Type attributeInterfaceType = s_NameToAttributeInterface[type];
			return ReflectionUtils.CreateInstance(attributeInterfaceType, m_Device, instanceTag) as AbstractAttributeInterface;
		}

		/// <summary>
		/// Uses reflection to instantiate the service with the given instance tag.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <returns></returns>
		private AbstractService InstantiateService(string instanceTag)
		{
			Type serviceType = s_ServiceTags[instanceTag];
			return ReflectionUtils.CreateInstance(serviceType, m_Device) as AbstractService;
		}

		/// <summary>
		/// Gets the hardcoded instance tag for the given service type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		private static string GetInstanceTagForService<T>()
			where T : AbstractService
		{
			Type type = typeof(T);
			return s_ServiceTags.GetKey(type);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.IndexNodeMap("AttributeInterfaces", GetAttributeInterfaces());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("ClearCache", "Clears all of the attribute interfaces", () => ClearCache());
			yield return new GenericConsoleCommand<string, string>("LoadAttributeInterface",
			                                                       "LoadAttributeInterface <TYPE> <TAG>",
			                                                       (type, tag) => LazyLoadAttributeInterface(type, tag));
			yield return new GenericConsoleCommand<string>("LoadService", "LoadService <TAG>", tag => LazyLoadService(tag));
		}

		#endregion
	}
}
