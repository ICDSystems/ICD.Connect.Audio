using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Microphone;
using ICD.Connect.Audio.Devices.Microphones;
using ICD.Connect.Audio.Shure.Controls;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Shure.Devices
{
	public abstract class AbstractShureMicDevice<TSettings> : AbstractMicrophoneDevice<TSettings>, IShureMicDevice
		where TSettings : AbstractShureMicDeviceSettings, new()
	{
		/// <summary>
		/// Raised when the mute button is pressed/released.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteButtonStatusChanged;

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly ShureMicSerialBuffer m_SerialBuffer;

		private bool m_MuteButtonStatus;

		/// <summary>
		/// Gets the mute button state.
		/// </summary>
		public bool MuteButtonStatus
		{
			get { return m_MuteButtonStatus; }
			private set
			{
				if (value == m_MuteButtonStatus)
					return;

				m_MuteButtonStatus = value;

				OnMuteButtonStatusChanged.Raise(this, new BoolEventArgs(m_MuteButtonStatus));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractShureMicDevice()
		{
			m_SerialBuffer = new ShureMicSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this);
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnMuteButtonStatusChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_SerialBuffer);

			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.Dispose();
		}

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetGainLevel(float volume)
		{
			volume = MathUtils.Clamp(volume, 0, 1400);

			ShureMicSerialData command = new ShureMicSerialData
			{
				Type = ShureMicSerialData.SET,
				Channel = 0,
				Command = "AUDIO_GAIN_HI_RES",
				Value = ((int)volume).ToString()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the muted state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetMuted(bool mute)
		{
			ShureMicSerialData command = new ShureMicSerialData
			{
				Type = ShureMicSerialData.SET,
				Command = "DEVICE_AUDIO_MUTE",
				Value = mute ? "ON" : "OFF"
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		public override void SetPhantomPower(bool power)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="brightness"></param>
		public abstract void SetLedStatus(eLedColor color, eLedBrightness brightness);

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		/// <summary>
		/// Sets the port for serial communication.
		/// </summary>
		/// <param name="port"></param>
		private void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port, false);
		}

		#endregion

		/// <summary>
		/// Sends the message to the device.
		/// </summary>
		/// <param name="message"></param>
		protected void Send(string message)
		{
			m_ConnectionStateManager.Send(message + "\r\n");
		}

		#region Port Callbacks

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when the port connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs e)
		{
			m_SerialBuffer.Clear();

			// Get the current state of the device
			if (e.Data)
				Send("< GET 0 ALL >");
		}

		/// <summary>
		/// Called when we receive data from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PortOnSerialDataReceived(object sender, StringEventArgs e)
		{
			m_SerialBuffer.Enqueue(e.Data);
		}

		#endregion

		#region Serial Buffer Callbacks

		/// <summary>
		/// Subscribe to the serial buffer events.
		/// </summary>
		/// <param name="serialBuffer"></param>
		private void Subscribe(ISerialBuffer serialBuffer)
		{
			serialBuffer.OnCompletedSerial += SerialBufferOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the serial buffer events.
		/// </summary>
		/// <param name="serialBuffer"></param>
		private void Unsubscribe(ISerialBuffer serialBuffer)
		{
			serialBuffer.OnCompletedSerial -= SerialBufferOnCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a complete message from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void SerialBufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			// Not sure why we get these messages - some kind of null-op?
			if (stringEventArgs.Data == "< REP ERR >")
				return;

			ShureMicSerialData response;

			try
			{
				response = ShureMicSerialData.Deserialize(stringEventArgs.Data);
			}
			catch (FormatException)
			{
				Logger.Log(eSeverity.Error, "Failed to parse message \"{0}\"", stringEventArgs.Data);
				return;
			}

			switch (response.Type)
			{
				case ShureMicSerialData.REP:

					switch (response.Command)
					{
						case "MUTE_BUTTON_STATUS":
							MuteButtonStatus = response.Value == "ON";
							break;

						case "DEVICE_AUDIO_MUTE":
							IsMuted = response.Value == "ON";
							break;

						case "AUDIO_GAIN_HI_RES":
							GainLevel = float.Parse(response.Value);
							break;
					}

					break;
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
				}
			}

			SetPort(port);
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

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(TSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new ShureMicRouteSourceControl(this, 0));
			addControl(new MicrophoneDeviceControl(this, 1));
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
				yield return node;

			if (m_ConnectionStateManager != null)
				yield return m_ConnectionStateManager.Port;

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
