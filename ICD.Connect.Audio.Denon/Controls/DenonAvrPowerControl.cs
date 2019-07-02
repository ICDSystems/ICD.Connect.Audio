using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Denon.Devices;
using ICD.Connect.Devices.Controls;

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
			Subscribe(parent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
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

		private void Subscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
			parent.OnDataReceived += ParentOnOnDataReceived;
		}

		private void Unsubscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged -= ParentOnOnInitializedChanged;
			parent.OnDataReceived -= ParentOnOnDataReceived;
		}

		private void ParentOnOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			switch (response.GetCommand())
			{
				case POWER_ON:
					IsPowered = true;
					break;

				case POWER_OFF:
					IsPowered = false;
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
