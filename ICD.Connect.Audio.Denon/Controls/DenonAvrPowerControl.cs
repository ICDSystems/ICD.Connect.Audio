using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Denon.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Audio.Denon.Controls
{
    public sealed class DenonAvrPowerControl : AbstractPowerDeviceControl<DenonAvrDevice>
	{
		private const string POWER = "PW";
		private const string POWER_ON = POWER + "ON";
		private const string POWER_OFF = POWER + "STANDBY";

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DenonAvrPowerControl(DenonAvrDevice parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		protected override void PowerOnFinal()
		{
			DenonSerialData data = DenonSerialData.Command(POWER_ON);
			Parent.SendData(data);
		}

		protected override void PowerOffFinal()
		{
			DenonSerialData data = DenonSerialData.Command(POWER_OFF);
			Parent.SendData(data);
		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(DenonAvrDevice parent)
		{
			base.Subscribe(parent);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
			parent.OnDataReceived += ParentOnOnDataReceived;
		}

		protected override void Unsubscribe(DenonAvrDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnInitializedChanged -= ParentOnOnInitializedChanged;
			parent.OnDataReceived -= ParentOnOnDataReceived;
		}

		private void ParentOnOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			switch (response.GetCommand())
			{
				case POWER_ON:
					PowerState = ePowerState.PowerOn;
					break;

				case POWER_OFF:
					PowerState = ePowerState.PowerOff;
					break;
			}
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			DenonSerialData data = DenonSerialData.Request(POWER);
			Parent.SendData(data);
		}

		#endregion
	}
}
