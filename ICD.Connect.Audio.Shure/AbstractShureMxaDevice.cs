using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Shure
{
	public abstract class AbstractShureMxaDevice<TSettings> : AbstractDevice<TSettings>, IShureMxaDevice
		where TSettings : AbstractShureMxaDeviceSettings, new()
	{
		/// <summary>
		/// Raised when the mute button is pressed/released.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteButtonStatusChanged; 

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly ShureMxaSerialBuffer m_SerialBuffer;

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
		protected AbstractShureMxaDevice()
		{
			m_SerialBuffer = new ShureMxaSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this);
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			Controls.Add(new ShureMxaRouteSourceControl(this, 0));
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
		/// Sets the brightness of the hardware LED.
		/// </summary>
		/// <param name="brightness"></param>
		public void SetLedBrightness(eLedBrightness brightness)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_BRIGHTNESS",
				Value = ((int)brightness).ToString()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is muted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedMuteColor(eLedColor color)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_COLOR_MUTED",
				Value = color.ToString().ToUpper()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is unmuted.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedUnmuteColor(eLedColor color)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "LED_COLOR_UNMUTED",
				Value = color.ToString().ToUpper()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Turns Metering On.
		/// </summary>
		/// <param name="milliseconds"></param>
		public void TurnMeteringOn(uint milliseconds)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "METER_RATE",
				Value = milliseconds.ToString()
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Sets the color of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		public void SetLedColor(eLedColor color)
		{
			SetLedMuteColor(color);
			SetLedUnmuteColor(color);
		}

		/// <summary>
		/// Sets the color and brightness of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="brightness"></param>
		public void SetLedColor(eLedColor color, eLedBrightness brightness)
		{
			if (brightness == eLedBrightness.Disabled)
			{
				SetLedBrightness(brightness);
				SetLedColor(color);
			}
			else
			{
				SetLedColor(color);
				SetLedBrightness(brightness);
			}
		}

		/// <summary>
		/// Enables/disables LED flashing.
		/// </summary>
		/// <param name="on"></param>
		public void SetLedFlash(bool on)
		{
			ShureMxaSerialData command = new ShureMxaSerialData
			{
				Type = ShureMxaSerialData.SET,
				Command = "FLASH",
				Value = on ? "ON" : "OFF"
			};

			Send(command.Serialize());
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		#endregion

		/// <summary>
		/// Sends the message to the device.
		/// </summary>
		/// <param name="message"></param>
		private void Send(string message)
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
			ShureMxaSerialData response = ShureMxaSerialData.Deserialize(stringEventArgs.Data);

			switch (response.Type)
			{
				case ShureMxaSerialData.REP:

					switch (response.Command)
					{
						case "MUTE_BUTTON_STATUS":
							MuteButtonStatus = response.Value == "ON";
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

			m_ConnectionStateManager.SetPort(null);
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
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
			}

			m_ConnectionStateManager.SetPort(port);
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

			string setLedBrightnessHelp =
				string.Format("SetLedBrightness <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eLedBrightness>()));

			yield return new GenericConsoleCommand<eLedBrightness>("SetLedBrightness", setLedBrightnessHelp, e => SetLedBrightness(e));

			string colorEnumString = string.Format("<{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eLedColor>()));

			yield return new GenericConsoleCommand<eLedColor>("SetLedColor", "SetLedColor " + colorEnumString, e => SetLedColor(e));
			yield return new GenericConsoleCommand<eLedColor>("SetLedMuteColor", "SetLedMuteColor " + colorEnumString, e => SetLedMuteColor(e));
			yield return new GenericConsoleCommand<eLedColor>("SetLedUnmuteColor", "SetLedUnmuteColor " + colorEnumString, e => SetLedUnmuteColor(e));
			yield return new GenericConsoleCommand<bool>("SetLedFlash", "SetLedFlash <true/false>", o => SetLedFlash(o));
			yield return new GenericConsoleCommand<uint>("TurnMeteringOn", "TurnMeteringOn <uint>", o => TurnMeteringOn(o));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
