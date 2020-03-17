using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Shure.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Shure.Devices
{
	public abstract class AbstractShureMicDevice<TSettings> : AbstractDevice<TSettings>, IShureMicDevice
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

			Controls.Add(new ShureMicRouteSourceControl(this, 0));
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
			ShureMicSerialData response = ShureMicSerialData.Deserialize(stringEventArgs.Data);

			switch (response.Type)
			{
				case ShureMicSerialData.REP:

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
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
				}
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
