using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Avr.Onkyo.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Avr.Onkyo
{
    public sealed class OnkyoAvrDevice : AbstractDevice<OnkyoAvrDeviceSettings>
    {
        public event EventHandler<PowerDeviceControlPowerStateApiEventArgs> OnPowerStateChange; 

        private const long COMMAND_DELAY_MS = 50;
        private readonly ISerialBuffer m_SerialBuffer;
        private readonly SerialQueue m_SerialQueue;
        private readonly ConnectionStateManager m_ConnectionStateManager;

        private readonly NetworkProperties m_NetworkProperties;
        private readonly ComSpecProperties m_ComSpecProperties;

        private readonly Dictionary<eOnkyoCommand, IcdHashSet<ResponseParserCallback>> m_ParserCallbacks;
        private readonly SafeCriticalSection m_ParserCallbackSection;

        private ePowerState m_PowerState;

        /// <summary>
        /// Delegate for parser callbacks
        /// </summary>
        public delegate void ResponseParserCallback(eOnkyoCommand responseCommand, string responseParameter, ISerialData sentData);

        /// <summary>
        /// Max possible volume. Varies between models
        /// Typically 80, but sometimes 50, 100, or 200
        /// I haven't found a reliable way of determining this automatically
        /// </summary>
        public int MaxVolume { get; private set; }
        
        public ePowerState PowerState
        {
            get { return m_PowerState; }
            private set
            {
                if (m_PowerState == value)
                    return;

                m_PowerState = value;

                OnPowerStateChange.Raise(this, new PowerDeviceControlPowerStateApiEventArgs(value));
            }
        }
        
        
        /// <summary>
		/// Constructor.
		/// </summary>
		public OnkyoAvrDevice()
        {
            m_NetworkProperties = new NetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

            m_ParserCallbacks = new Dictionary<eOnkyoCommand, IcdHashSet<ResponseParserCallback>>();
            m_ParserCallbackSection = new SafeCriticalSection();

        	m_SerialQueue = new SerialQueue
        	{
        	    CommandDelayTime = COMMAND_DELAY_MS
        	};
            Subscribe(m_SerialQueue);

            m_SerialBuffer = new DelimiterSerialBuffer(OnkyoIscpCommand.DELIMITERS);
            m_SerialQueue.SetBuffer(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
        }

        /// <summary>
        /// Gets the current online status of the device.
        /// </summary>
        /// <returns></returns>
        protected override bool GetIsOnlineStatus()
        {
            return m_ConnectionStateManager.IsConnected;
        }

        #region Methods

        /// <summary>
        /// Release resources.
        /// </summary>
        protected override void DisposeFinal(bool disposing)
        {
            base.DisposeFinal(disposing);

            Unsubscribe(m_SerialQueue);
        }

        /// <summary>
        /// Connect to the device.
        /// </summary>
        [PublicAPI]
        public void Connect()
        {
            m_ConnectionStateManager.Connect();
        }

        /// <summary>
        /// Disconnect from the device.
        /// </summary>
        [PublicAPI]
        public void Disconnect()
        {
            m_ConnectionStateManager.Disconnect();
        }

        /// <summary>
        /// Sets the port for communicating with the device.
        /// </summary>
        /// <param name="port"></param>
        [PublicAPI]
        public void SetPort(ISerialPort port)
        {
            m_ConnectionStateManager.SetPort(port, false);
            m_SerialQueue.SetPort(port);
        }

        /// <summary>
        /// Configures the given port for communication with the device.
        /// </summary>
        /// <param name="port"></param>
        private void ConfigurePort(IPort port)
        {
            // Com
            if (port is IComPort)
                (port as IComPort).ApplyDeviceConfiguration(m_ComSpecProperties);

            // TCP
            if (port is INetworkPort)
                (port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
        }

        public void SendCommand(OnkyoIscpCommand command)
        {
            command.AddEthernetHeader = (m_ConnectionStateManager.Port is INetworkPort);

            m_SerialQueue.Enqueue(command);
        }

        public void RegisterCommandCallback(eOnkyoCommand command, ResponseParserCallback callback)
        {
            m_ParserCallbackSection.Enter();

            try
            {
                var commandCallbacks = m_ParserCallbacks.GetOrAddNew(command, () => new IcdHashSet<ResponseParserCallback>());
                commandCallbacks.Add(callback);
            }
            finally
            {
                m_ParserCallbackSection.Leave();
            }
        }

        public void UnregisterCommandCallback(eOnkyoCommand command, ResponseParserCallback callback)
        {
            m_ParserCallbackSection.Enter();
            try
            {
                IcdHashSet<ResponseParserCallback> commandCallbacks;
                if (m_ParserCallbacks.TryGetValue(command, out commandCallbacks)) 
                    commandCallbacks.Remove(callback);
            }
            finally
            {
                m_ParserCallbackSection.Leave();
            }
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
            string response = args.Response.Trim();
            if (response.StartsWith(OnkyoIscpCommand.ISCP_HEADER))
            {
                //Response is eISCP, strip the header
                response = response.Substring(16);
            }

            string commandString = response.Substring(2, 3);
            string parameters = response.Substring(5);

            eOnkyoCommand command;

            if (!OnkyoCommandUtils.TryGetCommandForString(commandString, out command))
                return;

            IcdHashSet<ResponseParserCallback> commandCallbacks;

            m_ParserCallbackSection.Enter();
            try
            {
                if (!m_ParserCallbacks.TryGetValue(command, out commandCallbacks))
                    return;
            }
            finally
            {
                m_ParserCallbackSection.Leave();
            }

            foreach (var callback in commandCallbacks) 
                callback(command, parameters, args.Data);
        }

        /// <summary>
        /// Called when a command sent to the device did not receive a response soon enough.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void QueueOnTimeout(object sender, SerialDataEventArgs args)
        {
            Logger.Log(eSeverity.Error, "Timeout - {0}", args.Data.Serialize().TrimEnd(OnkyoIscpCommand.DELIMITERS));
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
            UpdateCachedOnlineStatus();
            if (args.Data)
                return;

            m_SerialBuffer.Clear();

            Logger.Log(eSeverity.Error, "Lost connection");
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
        
        #region PowerControlCallbacks

        private void Subscribe(OnkyoAvrPowerControl control)
        {
            control.OnPowerStateChanged += PowerControlOnOnPowerStateChanged;
        }

        private void PowerControlOnOnPowerStateChanged(object sender, PowerDeviceControlPowerStateApiEventArgs args)
        {
            PowerState = args.Data.PowerState;
        }

        #endregion

        #region Settings

        /// <summary>
        /// Override to clear the instance settings.
        /// </summary>
        protected override void ClearSettingsFinal()
        {
            base.ClearSettingsFinal();
            
            MaxVolume = OnkyoAvrDeviceSettings.DEFAULT_MAX_VOLUME;

            m_ComSpecProperties.ClearComSpecProperties();
            m_NetworkProperties.ClearNetworkProperties();

            SetPort(null);
        }

        /// <summary>
        /// Override to apply properties to the settings instance.
        /// </summary>
        /// <param name="settings"></param>
        protected override void CopySettingsFinal(OnkyoAvrDeviceSettings settings)
        {
            base.CopySettingsFinal(settings);

            settings.Port = m_ConnectionStateManager.PortNumber;
            settings.MaxVolume = MaxVolume;

            settings.Copy(m_ComSpecProperties);
            settings.Copy(m_NetworkProperties);
        }

        /// <summary>
        /// Override to apply settings to the instance.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        protected override void ApplySettingsFinal(OnkyoAvrDeviceSettings settings, IDeviceFactory factory)
        {
            base.ApplySettingsFinal(settings, factory);

            MaxVolume = settings.MaxVolume;

            m_ComSpecProperties.Copy(settings);
            m_NetworkProperties.Copy(settings);

            ISerialPort port = null;

            if (settings.Port != null)
            {
                try
                {
                    port = factory.GetPortById((int)settings.Port) as ISerialPort;
                }
                catch (KeyNotFoundException)
                {
                    Logger.Log(eSeverity.Error, "No serial port with id {0}", settings.Port);
                }
            }

            SetPort(port);
        }

        /// <summary>
        /// Override to add controls to the device.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="addControl"></param>
        protected override void AddControls(OnkyoAvrDeviceSettings settings, IDeviceFactory factory,
                                            Action<IDeviceControl> addControl)
        {
            base.AddControls(settings, factory, addControl);

            
            addControl(new OnkyoAvrRouteSwitcherControl(this, 0));
            var powerControl = new OnkyoAvrPowerControl(this, 1);
            addControl(powerControl);
            addControl(new OnkyoAvrVolumeControl(this, 2));
            
            Subscribe(powerControl);
        }

        /// <summary>
        /// Override to add actions on StartSettings
        /// This should be used to start communications with devices and perform initial actions
        /// </summary>
        protected override void StartSettingsFinal()
        {
            base.StartSettingsFinal();

            m_ConnectionStateManager.Start();
        }

        #endregion

        #region Console

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand command in GetBaseConsoleCommands())
                yield return command;

            yield return new GenericConsoleCommand<int>("SetMaxVolume", "Sets Max Volume", v => MaxVolume = v);

            yield return new ConsoleCommand("NRI", "NRI", () => SendCommand(OnkyoIscpCommand.ReceiverInformationQuery()));

            yield return new ConsoleCommand("InputQuery", "Input Query",
                () => SendCommand(OnkyoIscpCommand.InputQuery()));

            yield return new ConsoleCommand("Query", "Query All", () => Query());
        }

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
            foreach (var node in GetBaseConsoleNodes())
                yield return node;
        }

        private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
        {
            return base.GetConsoleNodes();
        }

        /// <summary>
        /// Calls the delegate for each console status item.
        /// </summary>
        /// <param name="addRow"></param>
        public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
        {
            base.BuildConsoleStatus(addRow);

            addRow("MaxVolume", MaxVolume);
        }

        private void Query()
        {
            SendCommand(OnkyoIscpCommand.PowerQuery());
            SendCommand(OnkyoIscpCommand.VolumeQuery());
            SendCommand(OnkyoIscpCommand.MuteQuery());
        }

        #endregion
    }
}