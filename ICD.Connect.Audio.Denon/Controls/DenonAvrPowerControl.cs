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
		public DenonAvrPowerControl(DenonAvrDevice parent)
			: base(parent, 1)
		{
		}

		public override void PowerOn()
		{
			DenonSerialData data = DenonSerialData.Command(POWER_ON);
			Parent.SendData(data);
		}

		public override void PowerOff()
		{
			DenonSerialData data = DenonSerialData.Command(POWER_OFF);
			Parent.SendData(data);
		}
	}
}
