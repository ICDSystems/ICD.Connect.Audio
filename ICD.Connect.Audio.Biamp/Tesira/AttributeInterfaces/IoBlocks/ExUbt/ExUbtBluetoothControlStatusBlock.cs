using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.Tesira.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.ExUbt
{
	public sealed class ExUbtBluetoothControlStatusBlock : AbstractIoBlock
	{
		private const string BLUETOOTH_DEVICE_NAME_ATTRIBUTE = "deviceName";
		private const string BLUETOOTH_MAC_ADDRESS_ATTRIBUTE = "deviceMAC";
		private const string BLUETOOTH_DISCOVERABLE_ATTRIBUTE = "discoverable";
		private const string BLUETOOTH_ENABLED_ATTRIBUTE = "enable";
		private const string BLUETOOTH_INACTIVITY_TIMEOUT_ATTRIBUE = "inactivityTimeout";
		private const string CONNECTED_DEVICE_NAME_ATTRIBUTE = "connectedDeviceName";
		private const string STREAMING_PROFILE_ATTRIBUTE = "profile";
		private const string CONNECTION_STATUS_ATTRIBUTE = "connected";
		private const string STREAMING_STATUS_ATTRIBUTE = "streaming";
		private const string DISCONNECT_SERVICE = "disconnect";

		#region Fields

		private string m_BluetoothDeviceName;
		private string m_BluetoothMacAddress;
		private bool m_BluetoothDiscoverable;
		private bool m_BluetoothEnabled;
		private int m_BluetoothInactivityTimeout;
		private string m_ConnectedDeviceName;
		private string m_StreamingProfile;
		private bool m_ConnectionStatus;
		private bool m_StreamingStatus;

		#endregion

		#region Events

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnBluetoothDeviceNameChanged;
		
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnBluetoothMacAddressChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnBluetoothDiscoverableChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnBluetoothEnabledChanged;

		[PublicAPI]
		public event EventHandler<IntEventArgs> OnBluetoothInactivityTimeoutChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnConnectedDeviceNameChanged;

		[PublicAPI]
		public event EventHandler<StringEventArgs> OnStreamingProfileChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnConnectionStatusChanged;

		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnStreamingStatusChanged;

		#endregion

		#region Properties

		[PublicAPI]
		public string BluetoothDeviceName
		{
			get { return m_BluetoothDeviceName; }
			private set
			{
				if (m_BluetoothDeviceName == value)
					return;

				m_BluetoothDeviceName = value;

				OnBluetoothDeviceNameChanged.Raise(this, m_BluetoothDeviceName);
			}
		}

		[PublicAPI]
		public string BluetoothMacAddress
		{
			get { return m_BluetoothMacAddress; }
			private set
			{
				if (m_BluetoothMacAddress == value)
					return;

				m_BluetoothMacAddress = value;

				OnBluetoothMacAddressChanged.Raise(this, m_BluetoothMacAddress);
			}
		}

		[PublicAPI]
		public bool BluetoothDiscoverable
		{
			get { return m_BluetoothDiscoverable; }
			private set
			{
				if (m_BluetoothDiscoverable == value)
					return;

				m_BluetoothDiscoverable = value;

				OnBluetoothDiscoverableChanged.Raise(this, m_BluetoothDiscoverable);
			}
		}

		[PublicAPI]
		public bool BluetoothEnabled
		{
			get { return m_BluetoothEnabled; }
			private set
			{
				if (m_BluetoothEnabled == value)
					return;

				m_BluetoothEnabled = value;

				OnBluetoothEnabledChanged.Raise(this, m_BluetoothEnabled);
			}
		}

		[PublicAPI]
		public int BluetoothInactivityTimeout
		{
			get { return m_BluetoothInactivityTimeout; }
			private set
			{
				if (m_BluetoothInactivityTimeout == value)
					return;

				m_BluetoothInactivityTimeout = value;

				OnBluetoothInactivityTimeoutChanged.Raise(this, m_BluetoothInactivityTimeout);
			}
		}

		[PublicAPI]
		public string ConnectedDeviceName
		{
			get { return m_ConnectedDeviceName; }
			private set
			{
				if (m_ConnectedDeviceName == value)
					return;

				m_ConnectedDeviceName = value;

				OnConnectedDeviceNameChanged.Raise(this, m_ConnectedDeviceName);
			}
		}

		[PublicAPI]
		public string StreamingProfile
		{
			get { return m_StreamingProfile; }
			private set
			{
				if (m_StreamingProfile == value)
					return;

				m_StreamingProfile = value;

				OnStreamingProfileChanged.Raise(this, m_StreamingProfile);
			}
		}

		[PublicAPI]
		public bool ConnectionStatus
		{
			get { return m_ConnectionStatus; }
			private set
			{
				if (m_ConnectionStatus == value)
					return;

				m_ConnectionStatus = value;

				OnConnectionStatusChanged.Raise(this, m_ConnectionStatus);
			}
		}

		[PublicAPI]
		public bool StreamingStatus
		{
			get { return m_StreamingStatus; }
			private set
			{
				if (m_StreamingStatus == value)
					return;

				m_StreamingStatus = value;

				OnStreamingStatusChanged.Raise(this, m_StreamingStatus);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public ExUbtBluetoothControlStatusBlock(BiampTesiraDevice device, string instanceTag) : base(device, instanceTag)
		{
			if (device.Initialized)
				Initialize();
		}

		/// <summary>
		/// Override to request initial values from the device, and subscribe for feedback.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			RequestAttribute(BluetoothDeviceNameCallback, AttributeCode.eCommand.Get, BLUETOOTH_DEVICE_NAME_ATTRIBUTE, null );
			RequestAttribute(BluetoothMacAddressCallback, AttributeCode.eCommand.Get, BLUETOOTH_MAC_ADDRESS_ATTRIBUTE, null);
			RequestAttribute(BluetoothDiscoverableCallback, AttributeCode.eCommand.Get, BLUETOOTH_DISCOVERABLE_ATTRIBUTE, null);
			RequestAttribute(BluetoothEnabledCallback, AttributeCode.eCommand.Get, BLUETOOTH_ENABLED_ATTRIBUTE, null);
			RequestAttribute(BluetoothInactivityTimeoutCallback, AttributeCode.eCommand.Get, BLUETOOTH_INACTIVITY_TIMEOUT_ATTRIBUE, null);
			RequestAttribute(ConnectedDeviceNameCallabck, AttributeCode.eCommand.Get, CONNECTED_DEVICE_NAME_ATTRIBUTE, null);
			RequestAttribute(StreamingProfileCallback, AttributeCode.eCommand.Get, STREAMING_PROFILE_ATTRIBUTE, null);
			RequestAttribute(ConnectionStatusCallback, AttributeCode.eCommand.Get, CONNECTION_STATUS_ATTRIBUTE, null);
			RequestAttribute(StreamingStatusCallback, AttributeCode.eCommand.Get, STREAMING_STATUS_ATTRIBUTE, null);
		}

		/// <summary>
		/// Subscribe/unsubscribe to the system using the given command type.
		/// </summary>
		/// <param name="command"></param>
		protected override void Subscribe(AttributeCode.eCommand command)
		{
			base.Subscribe(command);

			RequestAttribute(BluetoothMacAddressCallback, command, BLUETOOTH_MAC_ADDRESS_ATTRIBUTE, null);
			RequestAttribute(BluetoothDiscoverableCallback, command, BLUETOOTH_DISCOVERABLE_ATTRIBUTE, null);
			RequestAttribute(BluetoothEnabledCallback, command, BLUETOOTH_ENABLED_ATTRIBUTE, null);
			RequestAttribute(ConnectedDeviceNameCallabck, command, CONNECTED_DEVICE_NAME_ATTRIBUTE, null);
			RequestAttribute(StreamingProfileCallback, command, STREAMING_PROFILE_ATTRIBUTE, null);
			RequestAttribute(ConnectionStatusCallback, command, CONNECTION_STATUS_ATTRIBUTE, null);
			RequestAttribute(StreamingStatusCallback, command, STREAMING_STATUS_ATTRIBUTE, null);
		}

		#region Public Methods

		[PublicAPI]
		public void Disconnect()
		{
			RequestService(DISCONNECT_SERVICE, null);
		}

		[PublicAPI]
		public void SetBluetoothDeviceName(string name)
		{
			RequestAttribute(BluetoothDeviceNameCallback, AttributeCode.eCommand.Set, BLUETOOTH_DEVICE_NAME_ATTRIBUTE, new Value(name));
		}

		[PublicAPI]
		public void SetBluetoothDiscoverable(bool state)
		{
			RequestAttribute(BluetoothDiscoverableCallback, AttributeCode.eCommand.Set, BLUETOOTH_DISCOVERABLE_ATTRIBUTE, new Value(state));
		}

		[PublicAPI]
		public void ToggleBluetoothDiscoverable()
		{
			RequestAttribute(BluetoothDiscoverableCallback, AttributeCode.eCommand.Toggle, BLUETOOTH_DISCOVERABLE_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetBluetoothEnabled(bool state)
		{
			RequestAttribute(BluetoothEnabledCallback, AttributeCode.eCommand.Set, BLUETOOTH_ENABLED_ATTRIBUTE, new Value(state));
		}

		[PublicAPI]
		public void ToggleBluetoothEnabled()
		{
			RequestAttribute(BluetoothEnabledCallback, AttributeCode.eCommand.Toggle, BLUETOOTH_ENABLED_ATTRIBUTE, null);
		}

		[PublicAPI]
		public void SetBluetoothInactivityTimeout(int timeout)
		{
			if (timeout < 0 || timeout > 1800)
				throw new ArgumentOutOfRangeException("timeout", "Timeout must be between 0-1800 seconds");

			RequestAttribute(BluetoothInactivityTimeoutCallback, AttributeCode.eCommand.Set, BLUETOOTH_INACTIVITY_TIMEOUT_ATTRIBUE, new Value(timeout));
		}

		#endregion

		#region Attribute Callbacks

		private void BluetoothDeviceNameCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			BluetoothDeviceName = innerValue.StringValue;
		}

		private void BluetoothMacAddressCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			BluetoothMacAddress = innerValue.StringValue;
		}

		private void BluetoothDiscoverableCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			BluetoothDiscoverable = innerValue.BoolValue;
		}

		private void BluetoothEnabledCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			BluetoothEnabled = innerValue.BoolValue;
		}

		private void BluetoothInactivityTimeoutCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			BluetoothInactivityTimeout = innerValue.IntValue;
		}

		private void ConnectedDeviceNameCallabck(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			ConnectedDeviceName = innerValue.StringValue;
		}

		private void StreamingProfileCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			StreamingProfile = innerValue.StringValue;
		}

		private void ConnectionStatusCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			ConnectionStatus = innerValue.BoolValue;
		}

		private void StreamingStatusCallback(BiampTesiraDevice sender, ControlValue value)
		{
			Value innerValue = value.GetValue<Value>("value");
			StreamingStatus = innerValue.BoolValue;
		}

		#endregion
	}
}