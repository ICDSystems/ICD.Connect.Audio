using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Biamp.Controls;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Biamp
{
	public sealed class BiampTesiraDevice : AbstractDevice<BiampTesiraDeviceSettings>
	{
		// How often to check the connection and reconnect if necessary.
		private const long CONNECTION_CHECK_MILLISECONDS = 30 * 1000;

		// Delay after connection before we start initializing
		// Ensures we catch any login messages
		private const long INITIALIZATION_DELAY = 3 * 1000;

		public delegate void SubscriptionCallback(BiampTesiraDevice sender, ControlValue value);

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the device becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private readonly Dictionary<string, IcdHashSet<SubscriptionCallback>> m_SubscriptionCallbacks;
		private readonly SafeCriticalSection m_SubscriptionCallbacksSection;

		private readonly SafeTimer m_ConnectionTimer;

		private ISerialPort m_Port;
		private readonly BiampTesiraSerialQueue m_SerialQueue;

		private bool m_IsConnected;
		private bool m_Initialized;

		private readonly AttributeInterfaceFactory m_AttributeInterfaces;

		private readonly SafeTimer m_InitializationTimer;

		// Used with settings
		private string m_Config;

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
					return;

				m_Initialized = value;

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		/// <summary>
		/// Returns true when the device is connected.
		/// </summary>
		[PublicAPI]
		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				UpdateCachedOnlineStatus();

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

		/// <summary>
		/// Provides features for lazy loading attribute interface blocks and services.
		/// </summary>
		public AttributeInterfaceFactory AttributeInterfaces { get { return m_AttributeInterfaces; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public BiampTesiraDevice()
		{
			m_SubscriptionCallbacks = new Dictionary<string, IcdHashSet<SubscriptionCallback>>();
			m_SubscriptionCallbacksSection = new SafeCriticalSection();

			m_ConnectionTimer = new SafeTimer(ConnectionTimerCallback, 0, CONNECTION_CHECK_MILLISECONDS);

			m_SerialQueue = new BiampTesiraSerialQueue
			{
				Timeout = 10 * 1000
			};
			m_SerialQueue.SetBuffer(new BiampTesiraSerialBuffer());

			m_AttributeInterfaces = new AttributeInterfaceFactory(this);

			m_InitializationTimer = SafeTimer.Stopped(Initialize);

			Subscribe(m_SerialQueue);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			base.DisposeFinal(disposing);

			m_ConnectionTimer.Dispose();

			Unsubscribe(m_SerialQueue);
			Unsubscribe(m_Port);

			m_AttributeInterfaces.Dispose();

			Disconnect();
		}

		/// <summary>
		/// Connect to the device.
		/// </summary>
		[PublicAPI]
		public void Connect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect, port is null");
				return;
			}

			m_Port.Connect();
			IsConnected = m_Port.IsConnected;
		}

		/// <summary>
		/// Disconnect from the device.
		/// </summary>
		[PublicAPI]
		public void Disconnect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to disconnect, port is null");
				return;
			}

			m_Port.Disconnect();
			IsConnected = m_Port.IsConnected;
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			if (port == m_Port)
				return;

			Unsubscribe(m_Port);

			m_Port = port;
			m_SerialQueue.SetPort(m_Port);

			Subscribe(m_Port);

			UpdateCachedOnlineStatus();
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
			string xml;

			try
			{
				xml = IcdFile.ReadToEnd(fullPath, Encoding.UTF8);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to load integration config {0} - {1}", fullPath, e.Message);
				return;
			}

			ParseXml(xml);
		}

		/// <summary>
		/// Parses the given config xml.
		/// </summary>
		/// <param name="xml"></param>
		private void ParseXml(string xml)
		{
			Controls.Clear();

			foreach (IDeviceControl control in ControlsXmlUtils.GetControlsFromXml(xml, m_AttributeInterfaces))
				Controls.Add(control);
		}

		#endregion

		#region Attribute Subscription

		/// <summary>
		/// Subscribe to feedback from the device.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		public void SubscribeAttribute(SubscriptionCallback callback, string instanceTag, string attribute, int[] indices)
		{
			string key = GenerateSubscriptionKey(instanceTag, attribute, indices);

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				
				if (!m_SubscriptionCallbacks.ContainsKey(key))
					m_SubscriptionCallbacks[key] = new IcdHashSet<SubscriptionCallback>();
				m_SubscriptionCallbacks[key].Add(callback);
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			AttributeCode code = AttributeCode.Subscribe(instanceTag, attribute, key, indices);
			SendData(callback, code);
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
			string key = GenerateSubscriptionKey(instanceTag, attribute, indices);

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				if (m_SubscriptionCallbacks.ContainsKey(key))
				{
					m_SubscriptionCallbacks[key].Remove(callback);
					if (m_SubscriptionCallbacks[key].Count == 0)
						m_SubscriptionCallbacks.Remove(key);
				}
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			// Don't bother trying to unsubscribe from the device if we aren't connected...
			if (!m_IsConnected)
				return;

			AttributeCode code = AttributeCode.Unsubscribe(instanceTag, attribute, key, indices);
			SendData(callback, code);
		}

		/// <summary>
		/// Generates a subscription key for the given attribute so we can match system feedback to a callback.
		/// </summary>
		/// <param name="instanceTag"></param>
		/// <param name="attribute"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		public static string GenerateSubscriptionKey(string instanceTag, string attribute, params int[] indices)
		{
			string indicesString = string.Join("-", indices.Select(i => i.ToString()).ToArray());
			return string.Format("{0}-{1}-{2}", instanceTag, attribute, indicesString);
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
		/// Sends the data to the device and calls the callback asynchronously with the response.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="data"></param>
		internal void SendData(SubscriptionCallback callback, ICode data)
		{
			if (!IsConnected)
			{
				Log(eSeverity.Warning, "Device disconnected, attempting reconnect");
				Connect();
			}

			if (!IsConnected)
			{
				Log(eSeverity.Critical, "Unable to communicate with device");
				return;
			}

			CodeCallbackPair pair = new CodeCallbackPair(data, callback);
			m_SerialQueue.Enqueue(pair);
		}

		/// <summary>
		/// Logs the message.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);
			message = AddLogPrefix(message);

			Logger.AddEntry(severity, message);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsConnected;
		}

		/// <summary>
		/// Initialize the device.
		/// </summary>
		private void Initialize()
		{
			Initialized = true;
		}

		/// <summary>
		/// Returns the log message with a LutronQuantumNwkDevice prefix.
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		private string AddLogPrefix(string log)
		{
			return string.Format("{0} - {1}", GetType().Name, log);
		}

		/// <summary>
		/// Called periodically to maintain connection to the device.
		/// </summary>
		private void ConnectionTimerCallback()
		{
			if (m_Port != null && !m_Port.IsConnected)
				Connect();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribes to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			IsConnected = args.Data;

			if (IsConnected)
				m_InitializationTimer.Reset(INITIALIZATION_DELAY);
			else
			{
				m_InitializationTimer.Stop();
				m_SerialQueue.Clear();

				Log(eSeverity.Critical, "Lost connection");
				Initialized = false;

				m_ConnectionTimer.Reset(CONNECTION_CHECK_MILLISECONDS, CONNECTION_CHECK_MILLISECONDS);
			}
		}

		/// <summary>
		/// Called when the port online status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Serial Queue Callbacks

		/// <summary>
		/// Subscribes to the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Subscribe(BiampTesiraSerialQueue queue)
		{
			queue.OnSerialResponse += QueueOnSerialResponse;
			queue.OnSubscriptionFeedback += QueueOnSubscriptionFeedback;
			queue.OnTimeout += QueueOnTimeout;
		}

		/// <summary>
		/// Unsubscribes from the queue events.
		/// </summary>
		/// <param name="queue"></param>
		private void Unsubscribe(BiampTesiraSerialQueue queue)
		{
			queue.OnSerialResponse -= QueueOnSerialResponse;
			queue.OnSubscriptionFeedback += QueueOnSubscriptionFeedback;
			queue.OnTimeout += QueueOnTimeout;
		}

		/// <summary>
		/// Called when we receive a response from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void QueueOnSerialResponse(object sender, SerialResponseEventArgs eventArgs)
		{
			try
			{
				CodeCallbackPair pair = eventArgs.Data as CodeCallbackPair;
				Response response = Deserialize(eventArgs.Response);

				switch (response.ResponseType)
				{
					case Response.eResponseType.Error:
					case Response.eResponseType.CannotDeliver:
					case Response.eResponseType.GeneralFailure:

						// This is a good thing!
						if (eventArgs.Response.Contains("ALREADY_SUBSCRIBED"))
							return;

						if (pair == null)
						{
							Log(eSeverity.Error, eventArgs.Response);
						}
						else
						{
							string tX = pair.Code.Serialize().TrimEnd(TtpUtils.CR, TtpUtils.LF);
							Log(eSeverity.Error, "{0} - {1}", tX, eventArgs.Response);
						}
						return;
				}

				// Don't bother handling responses with no associated values, e.g. "+OK"
				if (response.Values.Count == 0)
					return;

				if (pair != null && pair.Callback != null)
					SafeCallback(pair.Callback, response, eventArgs.Response);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to parse {0} - {1}", eventArgs.Data, e.Message);
			}
		}

		/// <summary>
		/// Called when we receive subscription feedback from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void QueueOnSubscriptionFeedback(object sender, StringEventArgs eventArgs)
		{
			Response response = Deserialize(eventArgs.Data);
			string key = response.PublishToken;
			SubscriptionCallback[] callbacksArray;

			m_SubscriptionCallbacksSection.Enter();

			try
			{
				IcdHashSet<SubscriptionCallback> callbacks;
				if (!m_SubscriptionCallbacks.TryGetValue(key, out callbacks))
					return;

				callbacksArray = callbacks.ToArray();
			}
			finally
			{
				m_SubscriptionCallbacksSection.Leave();
			}

			foreach (SubscriptionCallback callback in callbacksArray)
				SafeCallback(callback, response, eventArgs.Data);
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
		private void SafeCallback(SubscriptionCallback callback, Response response, string responseString)
		{
			try
			{
				callback(this, response.Values);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to execute callback for response {0} - {1}", responseString, e.Message);
			}
		}

		private Response Deserialize(string feedback)
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

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Config = null;
			Username = null;
			SetPort(null);
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
			settings.Port = m_Port == null ? (int?)null : m_Port.Id;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(BiampTesiraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Username = settings.Username;

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Log(eSeverity.Error, "No serial Port with id {0}", settings.Port);
			}

			SetPort(port);

			// Load the config
			if (!string.IsNullOrEmpty(settings.Config))
				LoadControls(settings.Config);
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

			addRow("Initialized", Initialized);
		}

		#endregion
	}
}
