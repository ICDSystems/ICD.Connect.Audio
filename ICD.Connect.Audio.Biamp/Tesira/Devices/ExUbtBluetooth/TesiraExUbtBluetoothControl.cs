using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces.IoBlocks.ExUbt;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Misc.Bluetooth;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices.ExUbtBluetooth
{
	public sealed class TesiraExUbtBluetoothControl : AbstractDeviceControl<TesiraExUbtBluetoothDevice>, IBluetoothConnectedControl, IBluetoothDiscoverableControl
	{
		#region Events

		public event EventHandler<BoolEventArgs> OnBluetoothConnectedStatusChanged;
		public event EventHandler<StringEventArgs> OnBluetootConnectedDeviceNameChanged;
		public event EventHandler<BoolEventArgs> OnBluetoothDiscoverableStatusChanged;
		public event EventHandler<StringEventArgs> OnBluetoothDiscoverableNameChanged;

		#endregion

		#region Fields

		private ExUbtBluetoothControlStatusBlock m_Block;
		private bool m_BluetoothConnectedStatus;
		private bool m_BluetoothDiscoverableStatus;
		private string m_BluetoothDiscoverableName;
		private string m_BluetoothConnectedDeviceName;

		#endregion

		#region Properties

		IBluetoothDevice IDeviceControl<IBluetoothDevice>.Parent { get { return Parent; } }

		public eBluetoothConnectedFeatures BluetoothConnectedFeatures
		{
			get
			{
				return eBluetoothConnectedFeatures.ConnectedDeviceName |
				       eBluetoothConnectedFeatures.ConnectedStatus |
				       eBluetoothConnectedFeatures.Disconnect;
			}
		}

		public eBluetoothDiscoverableFeatures BluetoothDiscoverableFeatures
		{
			get
			{
				return eBluetoothDiscoverableFeatures.GetName |
				       eBluetoothDiscoverableFeatures.StartStopDiscvoery;
			}
		}

		public bool BluetoothConnectedStatus
		{
			get { return m_BluetoothConnectedStatus; }
			private set
			{
				if (m_BluetoothConnectedStatus == value)
					return;

				m_BluetoothConnectedStatus = value;

				OnBluetoothConnectedStatusChanged.Raise(this, m_BluetoothConnectedStatus);
			}
		}

		public string BluetoothConnectedDeviceName
		{
			get { return m_BluetoothConnectedDeviceName; }
			private set
			{
				if (m_BluetoothConnectedDeviceName == value)
					return;

				m_BluetoothConnectedDeviceName = value;

				OnBluetootConnectedDeviceNameChanged.Raise(this, m_BluetoothConnectedDeviceName);
			}
		}

		public bool BluetoothDiscoverableStatus
		{
			get { return m_BluetoothDiscoverableStatus; }
			private set
			{
				if (m_BluetoothDiscoverableStatus == value)
					return;

				m_BluetoothDiscoverableStatus = value;

				OnBluetoothDiscoverableStatusChanged.Raise(this, m_BluetoothDiscoverableStatus);
			}
		}

		public string BluetoothDiscoverableName
		{
			get { return m_BluetoothDiscoverableName; }
			private set
			{
				if (m_BluetoothDiscoverableName == value)
					return;

				m_BluetoothDiscoverableName = value;

				OnBluetoothDiscoverableNameChanged.Raise(this, m_BluetoothDiscoverableName);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public TesiraExUbtBluetoothControl([NotNull] TesiraExUbtBluetoothDevice parent, int id) : base(parent, id)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		public TesiraExUbtBluetoothControl([NotNull] TesiraExUbtBluetoothDevice parent, int id, Guid uuid) : base(parent, id, uuid)
		{
		}

		#region Methods

		public void BluetoothDisconnect()
		{
			if (m_Block == null)
				throw new InvalidOperationException("Can't disconnect without an attribute interface attached");

			m_Block.Disconnect();
		}

		public void BluetoothDiscoverableStart(bool start)
		{
			if (m_Block == null)
				throw new InvalidOperationException("Can't set discoverable status without an attribute interface attached");

			m_Block.SetBluetoothDiscoverable(start);
		}

		public void SetBlock(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == m_Block)
				return;

			Unsubscribe(m_Block);
			m_Block = block;
			Subscribe(m_Block);

			UpdateStatus();
		}

		private void UpdateStatus()
		{
			BluetoothConnectedStatus = m_Block != null && m_Block.ConnectionStatus;
			BluetoothConnectedDeviceName = m_Block != null ? m_Block.ConnectedDeviceName : null;
			BluetoothDiscoverableStatus = m_Block != null && m_Block.BluetoothDiscoverable;
			BluetoothDiscoverableName = m_Block != null ? m_Block.BluetoothDeviceName : null;
		}

		#endregion

		#region Block Callbacks

		private void Subscribe(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == null)
				return;

			block.OnBluetoothDeviceNameChanged += BlockOnBluetoothDeviceNameChanged;
			block.OnBluetoothDiscoverableChanged += BlockOnBluetoothDiscoverableChanged;
			block.OnConnectedDeviceNameChanged += BlockOnConnectedDeviceNameChanged;
			block.OnConnectionStatusChanged += BlockOnConnectionStatusChanged;
		}

		private void Unsubscribe(ExUbtBluetoothControlStatusBlock block)
		{
			if (block == null)
				return;

			block.OnBluetoothDeviceNameChanged -= BlockOnBluetoothDeviceNameChanged;
			block.OnBluetoothDiscoverableChanged -= BlockOnBluetoothDiscoverableChanged;
			block.OnConnectedDeviceNameChanged -= BlockOnConnectedDeviceNameChanged;
			block.OnConnectionStatusChanged -= BlockOnConnectionStatusChanged;
		}

		private void BlockOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			BluetoothConnectedStatus = args.Data;
		}

		private void BlockOnConnectedDeviceNameChanged(object sender, StringEventArgs args)
		{
			BluetoothConnectedDeviceName = args.Data;
		}

		private void BlockOnBluetoothDiscoverableChanged(object sender, BoolEventArgs args)
		{
			BluetoothDiscoverableStatus = args.Data;
		}

		private void BlockOnBluetoothDeviceNameChanged(object sender, StringEventArgs args)
		{
			BluetoothDiscoverableName = args.Data;
		}

		#endregion
	}
}