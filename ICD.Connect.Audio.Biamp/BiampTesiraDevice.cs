using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Telemetry.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICD.Connect.Audio.Biamp
{
	[ExternalTelemetry("Biamp", typeof(BiampExternalTelemetryProvider))]
	public sealed class BiampTesiraDevice : AbstractDevice<BiampTesiraDeviceSettings>
	{
		/// <summary>
		/// Controls how long the biamp should wait between sending commands to avoid overloading the device
		/// </summary>
		[PublicAPI]
		public const long COMMAND_DELAY_MS = 150;

		/// <summary>
		/// Controls how long the biamp should wait before considering a command to have timed out
		/// </summary>
		[PublicAPI]
		public const long TIMEOUT_MS = 20 * 1000;

		// How often to re-subscribe to device events
		private const long SUBSCRIPTION_INTERVAL_MILLISECONDS = 10 * 60 * 1000;

		// Delay after connection before we start initializing
		// Ensures we catch any login messages
		private const long INITIALIZATION_DELAY_MILLISECONDS = 3 * 1000;

		internal const float TESIRA_LEVEL_MINIMUM = -100f;

		internal const float TESIRA_LEVEL_MAXIMUM = 20f;

		internal const int PRIORITY_SERVICE = 16;
		internal const int PRIORITY_SET = 32;
		internal const int PRIORITY_SUBSCRIBE_INITIAL = 64;
		internal const int PRIORITY_GET = 128;
		internal const int PRIORITY_SUBSCRIBE_PERODIC = 256;

		public delegate void SubscriptionCallback(BiampTesiraDevice sender, ControlValue value);

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private readonly Dictionary<string, IcdHashSet<SubscriptionCallbackInfo>> m_SubscriptionCallbacks;
		private readonly SafeCriticalSection m_SubscriptionCallbacksSection;

		private readonly SafeTimer m_SubscriptionTimer;
		private readonly SafeTimer m_InitializationTimer;

		private readonly SerialQueue m_SerialQueue;
		private readonly BiampTesiraSerialBuffer m_SerialBuffer;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		private bool m_Initialized;

		// Used with settings
		private string m_Config;
		private readonly IcdHashSet<IDeviceControl> m_LoadedControls;
		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;

		private bool m_ReadyToTransmit;

		#region Properties

		/// <summary>
		/// Username for logging in to the device.
		/// </summary>
		[PublicAPI]
		public string Username { get; set; }

		/// <summary>
		/// Device Initialized Status.
		/// </summary>
		[PublicAPI]
		public bool Initialized
		{
			get { return m_Initialized; }
			private set
			{
				if (value == m_Initialized)
				{
					return;
				}

				m_Initialized = value;

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		/// <summary>
		/// Provides features for lazy loading attribute interface blocks and services.
		/// </summary>
		[PublicAPI]
		public AttributeInterfaceFactory AttributeInterfaces { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public BiampTesiraDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			m_LoadedControls = new IcdHashSet<IDeviceControl>();

			Controls.Add(new BiampTesiraRoutingControl(this, 0));

			m_SubscriptionCallbacks = new Dictionary<string, IcdHashSet<SubscriptionCallbackInfo>>();
			m_SubscriptionCallbacksSection = new SafeCriticalSection();

			m_SubscriptionTimer = SafeTimer.Stopped(SubscriptionTimerCallback);
			m_InitializationTimer = SafeTimer.Stopped(Initialize);

			m_SerialQueue = new SerialQueue
			{
				Timeout = TIMEOUT_MS,
				CommandDelayTime = COMMAND_DELAY_MS
			};

			m_SerialBuffer = new BiampTesiraSerialBuffer();

			Subscribe(m_SerialBuffer);

			m_SerialQueue.SetBuffer(m_SerialBuffer);

			Subscribe(m_SerialQueue);

			AttributeInterfaces = new AttributeInterfaceFactory(this);

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;

			Unsubscribe(m_SerialQueue);
			Unsubscribe(m_SerialBuffer);

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.Dispose();

			base.DisposeFinal(disposing);

			AttributeInterfaces.Dispose();
		}

		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
			m_SerialQueue.SetPort(port);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(ISerialPort port)
		{
			// Com
			if (port is IComPort)
				(port as IComPort).ApplyDeviceConfiguration(m_ComSpecProperties);

			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
		}

		/// <summary>
		/// Loads the controls from the config at the given path.
		/// </summary>
		/// <param name="path"></param>
		[PublicAPI]
		public void LoadControls(string path)
		{
			m_Config = path;

			string fullPath = PathUtils.GetDefaultConfigPath("Tesira", path);

			try
			{
				string xml = IcdFile.ReadToEnd(fullPath, new UTF8Encoding(false));
				xml = EncodingUtils.StripUtf8Bom(xml);

				ParseXml(xml);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "{0} Failed to load integration config {1} - {2}", this, fullPath, e.Message);
			}
		}

		/// <summary>
		/// Parses the given config xml.
		/// </summary>
		/// <param name="xml"></param>
		private void ParseXml(string xml)
		{
			DisposeLoadedControls();

			// Load and add the new controls
			foreach (IDeviceControl control in ControlsXmlUtils.GetControlsFromXml(xml, AttributeInterfaces))
			{
				Controls.Add(control);
				m_LoadedControls.Add(control);
			}
		}

		#endregion

		#region Attribute Subscription

		public bool IsAttributeSubscribed(SubscriptionCallback callback, string instanceTag, string attribute,
		                                  int[] indices)
		{
			string key = SubscriptionCallbackInfo.GenerateSubscriptionKey(instanceTag, attribute, indices);
			AttributeCode code = AttributeCode.Subscribe(instanceTag, attribute, key, indices);
			m_SubscriptionCallbacksSection.Enter();

			try
			{
				IcdHashSet<SubscriptionCallbackInfo> infos;
				if (!m_SubscriptionCallbacks.TryGetValue(key, out infos))
				{
					return false;
				}

				if (infos.Any(s => s.Callback == callback && s.Code.CompareEquality(code)))
				{
					return true;
				}
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			return false;
		}

		/// <summary>
		/// Subscribe to feedback from the device.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		public void SubscribeAttribute(SubscriptionCallback callback, string instanceTag, string attribute, int[] indices)
		{
			string key = SubscriptionCallbackInfo.GenerateSubscriptionKey(instanceTag, attribute, indices);
			AttributeCode code = AttributeCode.Subscribe(instanceTag, attribute, key, indices);
			SubscriptionCallbackInfo info = new SubscriptionCallbackInfo(callback, code);

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				IcdHashSet<SubscriptionCallbackInfo> infos;
				if (!m_SubscriptionCallbacks.TryGetValue(key, out infos))
				{
					infos = new IcdHashSet<SubscriptionCallbackInfo>();
					m_SubscriptionCallbacks[key] = infos;
				}

				if (!infos.Any(s => s.Callback == callback && s.Code.CompareEquality(code)))
				{
					infos.Add(info);
				}
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			SendData(callback, code, PRIORITY_SUBSCRIBE_INITIAL);
		}

		/// <summary>
		/// Unsubscribe to feedback from the device.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		public void UnsubscribeAttribute(SubscriptionCallback callback, string instanceTag, string attribute, int[] indices)
		{
			string key = SubscriptionCallbackInfo.GenerateSubscriptionKey(instanceTag, attribute, indices);
			AttributeCode code = AttributeCode.Unsubscribe(instanceTag, attribute, key, indices);

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				IcdHashSet<SubscriptionCallbackInfo> infos;
				if (!m_SubscriptionCallbacks.TryGetValue(key, out infos))
				{
					return;
				}

				SubscriptionCallbackInfo remove = infos.FirstOrDefault(s => s.Callback == callback && s.Code.CompareEquality(code));
				if (remove == null)
				{
					return;
				}

				infos.Remove(remove);

				if (infos.Count == 0)
				{
					m_SubscriptionCallbacks.Remove(key);
				}
				else
				{
					return;
				}
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			// Don't bother trying to unsubscribe from the device if we aren't connected.
			if (!m_ConnectionStateManager.IsConnected)
			{
				return;
			}

			SendData(callback, code, PRIORITY_SUBSCRIBE_INITIAL);
		}

		/// <summary>
		/// Called periodically to enforce subscriptions to device events.
		/// </summary>
		private void SubscriptionTimerCallback()
		{
			if (!m_ConnectionStateManager.IsConnected)
			{
				return;
			}

			SubscriptionCallbackInfo[] subscriptions =
				m_SubscriptionCallbacksSection.Execute(() => m_SubscriptionCallbacks.SelectMany(kvp => kvp.Value).ToArray());

			foreach (SubscriptionCallbackInfo subscription in subscriptions)
			{
				SendData(subscription.Callback, subscription.Code, PRIORITY_SUBSCRIBE_PERODIC);
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Sends the data to the device.
		/// </summary>
		/// <param name="data"></param>
		internal void SendData(ICode data)
		{
			SendData(null, data);
		}

		/// <summary>
		/// Sends the data to the device, queued at the specified priority
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		internal void SendData(ICode data, int priority)
		{
			SendData(null, data, priority);
		}

		/// <summary>
		/// Sends the data to the device, queued at the lowest priority,
		/// and calls the callback asynchronously with the response.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="data"></param>
		internal void SendData(SubscriptionCallback callback, ICode data)
		{
			SendData(callback, data, int.MaxValue);
		}

		/// <summary>
		/// Sends the data to the device, queued at the specified priority,
		/// and calls the callback asynchronously with the response.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		internal void SendData(SubscriptionCallback callback, ICode data, int priority)
		{
			CodeCallbackPair pair = new CodeCallbackPair(data, callback);
			m_SerialQueue.EnqueuePriority(pair, (a, b) => a.Code.CompareEquality(b.Code),priority);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		/// <summary>
		/// Initialize the device.
		/// </summary>
		private void Initialize()
		{
			if (m_ReadyToTransmit)
			{
				Initialized = true;
			}
			else
			{
				m_InitializationTimer.Reset(INITIALIZATION_DELAY_MILLISECONDS);
			}
		}

		private void DisposeLoadedControls()
		{
			// Remove the previously loaded controls
			foreach (IDeviceControl control in m_LoadedControls)
			{
				Controls.Remove(control.Id);
				control.Dispose();
			}
			m_LoadedControls.Clear();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			if (args.Data)
			{
				m_SubscriptionTimer.Reset(0, SUBSCRIPTION_INTERVAL_MILLISECONDS);
				m_InitializationTimer.Reset(INITIALIZATION_DELAY_MILLISECONDS);
			}
			else
			{
				m_SubscriptionTimer.Stop();
				m_InitializationTimer.Stop();
				m_ReadyToTransmit = false;

				Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}
		}

		/// <summary>
		/// Called when the port online status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Serial Queue Callbacks

		/// <summary>
		/// Subscribes to the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Subscribe(SerialQueue queue)
		{
			queue.OnSerialResponse += QueueOnSerialResponse;
			queue.OnTimeout += QueueOnTimeout;
		}

		/// <summary>
		/// Unsubscribes from the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Unsubscribe(SerialQueue queue)
		{
			queue.OnSerialResponse -= QueueOnSerialResponse;
			queue.OnTimeout -= QueueOnTimeout;
		}

		/// <summary>
		/// Called when we receive a response from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void QueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			// Ignore any messages that don't fit expected pattern
			if (args.Response.StartsWith(Response.CANNOT_DELIVER) ||
			    args.Response.StartsWith(Response.ERROR) ||
			    args.Response.StartsWith(Response.GENERAL_FAILURE) ||
			    args.Response.StartsWith(Response.SUCCESS))
			{
				HandleFeedback(args.Data as CodeCallbackPair, args.Response);
			}
		}

		/// <summary>
		/// Called when we receive non-subscription feedback from the device
		/// </summary>
		/// <param name="pair"></param>
		/// <param name="feedback"></param>
		private void HandleFeedback(CodeCallbackPair pair, string feedback)
		{
			try
			{
				Response response = Deserialize(feedback);

				switch (response.ResponseType)
				{
					case Response.eResponseType.Error:
					case Response.eResponseType.CannotDeliver:
					case Response.eResponseType.GeneralFailure:

						// This is a good thing!
						if (feedback.Contains("ALREADY_SUBSCRIBED"))
						{
							return;
						}

						if (pair == null)
						{
							Log(eSeverity.Error, feedback);
						}
						else
						{
							string tX = pair.Code.Serialize().TrimEnd(TtpUtils.CR, TtpUtils.LF);
							Log(eSeverity.Error, "{0} - {1}", tX, feedback);
						}
						return;
				}

				// Don't bother handling responses with no associated values, e.g. "+OK"
				if (response.Values.Count == 0)
				{
					return;
				}

				if (pair != null && pair.Callback != null)
				{
					SafeExecuteCallback(pair.Callback, pair, response, feedback);
				}
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to parse {0} - {1}", pair, e.Message);
			}
		}

		/// <summary>
		/// Called when we receive subscription feedback from the device.
		/// </summary>
		/// <param name="feedback"></param>
		private void HandleSubscriptionFeedback(string feedback)
		{
			Response response = Deserialize(feedback);
			string key = response.PublishToken;
			SubscriptionCallback[] callbacks;

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				IcdHashSet<SubscriptionCallbackInfo> infos;
				if (!m_SubscriptionCallbacks.TryGetValue(key, out infos))
				{
					return;
				}

				callbacks = infos.Select(s => s.Callback)
										 .ToArray(infos.Count);
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			foreach (SubscriptionCallback callback in callbacks)
			{
				SafeExecuteCallback(callback, response, feedback);
			}
		}

		/// <summary>
		/// Called when a command sent to the device did not receive a response soon enough.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void QueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Error, "Timeout - {0}", args.Data.Serialize().TrimEnd(TtpUtils.CR, TtpUtils.LF));
		}

		/// <summary>
		/// Executes the callback for the given response. Logs exceptions instead of raising.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="response"></param>
		/// <param name="responseString"></param>
		private void SafeExecuteCallback(SubscriptionCallback callback, Response response, string responseString)
		{
			try
			{
				ExecuteCallback(callback, response);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to execute callback for response {0} - {1}{2}{3}",
					StringUtils.ToRepresentation(StringUtils.Trim(responseString)), e.Message,
					IcdEnvironment.NewLine, e.StackTrace);
			}
		}

		/// <summary>
		/// Executes the callback for the given response. Logs exceptions instead of raising.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="responseString"></param>
		private void SafeExecuteCallback(SubscriptionCallback callback, ISerialData request, Response response,
										 string responseString)
		{
			try
			{
				ExecuteCallback(callback, response);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to execute callback for request {0} response {1} - {2}{3}{4}",
					StringUtils.ToRepresentation(StringUtils.Trim(request.Serialize())),
					StringUtils.ToRepresentation(StringUtils.Trim(responseString)),
					e.Message, IcdEnvironment.NewLine, e.StackTrace);
			}
		}

		/// <summary>
		/// Executes the given callback.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="response"></param>
		private void ExecuteCallback(SubscriptionCallback callback, Response response)
		{
			callback(this, response.Values);
		}

		private static Response Deserialize(string feedback)
		{
			try
			{
				return Response.Deserialize(feedback);
			}
			catch (Exception e)
			{
				string message = string.Format("Failed to parse \"{0}\" - {1}", feedback, e.Message);
				throw new FormatException(message, e);
			}
		}

		#endregion

		#region Serial Buffer Callbacks

		private void Subscribe(BiampTesiraSerialBuffer buffer)
		{
			if (buffer == null)
			{
				return;
			}

			buffer.OnSerialTelnetHeader += BufferOnOnSerialTelnetHeader;
			buffer.OnWelcomeMessageReceived += BufferOnOnWelcomeMessageReceived;
			buffer.OnSubscribeResponse += BufferOnSubscribeResponse;
		}

		private void Unsubscribe(BiampTesiraSerialBuffer buffer)
		{
			if (buffer == null)
			{
				return;
			}

			buffer.OnSerialTelnetHeader -= BufferOnOnSerialTelnetHeader;
			buffer.OnWelcomeMessageReceived -= BufferOnOnWelcomeMessageReceived;
			buffer.OnSubscribeResponse -= BufferOnSubscribeResponse;
		}

		private void BufferOnOnSerialTelnetHeader(object sender, StringEventArgs args)
		{
			m_ConnectionStateManager.Send(TelnetControl.Reject(args.Data));
		}

		private void BufferOnOnWelcomeMessageReceived(object sender, EventArgs e)
		{
			m_ReadyToTransmit = true;
		}

		private void BufferOnSubscribeResponse(object sender, StringEventArgs e)
		{
			HandleSubscriptionFeedback(e.Data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Config = null;
			Username = null;
			DisposeLoadedControls();

			SetPort(null);

			m_NetworkProperties.ClearNetworkProperties();
			m_ComSpecProperties.ClearComSpecProperties();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(BiampTesiraDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Config = m_Config;
			settings.Username = Username;
			settings.Port = m_ConnectionStateManager.PortNumber;

			settings.Copy(m_NetworkProperties);
			settings.Copy(m_ComSpecProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(BiampTesiraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
			m_ComSpecProperties.Copy(settings);

			Username = settings.Username;

			// Load the config
			if (!string.IsNullOrEmpty(settings.Config))
			{
				LoadControls(settings.Config);
			}

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
				}
			}

			SetPort(port);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Loaded Config", m_Config);
			addRow("Connected", m_ConnectionStateManager.IsConnected);
			addRow("Initialized", Initialized);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
			{
				yield return command;
			}

			yield return new GenericConsoleCommand<string>("LoadControls", "LoadControls <PATH>", p => LoadControls(p));
			yield return new ConsoleCommand("ReloadControls", "Reloads the controls from the most recent path",
											() => LoadControls(m_Config));
			yield return new ConsoleCommand("Resubscribe", "Resends subscription commands to the device", () => SubscriptionTimerCallback());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
			{
				yield return node;
			}

			yield return ConsoleNodeGroup.IndexNodeMap("Attributes", AttributeInterfaces.GetAttributeInterfaces());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
