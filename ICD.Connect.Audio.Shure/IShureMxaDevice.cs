using ICD.Connect.Devices;
using ICD.Connect.Protocol.Heartbeat;

namespace ICD.Connect.Audio.Shure
{
	public interface IShureMxaDevice : IConnectable, IDevice
	{
		/// <summary>
		/// Sets the brightness of the hardware LED.
		/// </summary>
		/// <param name="brightness"></param>
		void SetLedBrightness(eLedBrightness brightness);

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is muted.
		/// </summary>
		/// <param name="color"></param>
		void SetLedMuteColor(eLedColor color);

		/// <summary>
		/// Sets the color of the hardware LED while the microphone is unmuted.
		/// </summary>
		/// <param name="color"></param>
		void SetLedUnmuteColor(eLedColor color);

		/// <summary>
		/// Sets the color of the hardware LED.
		/// </summary>
		/// <param name="color"></param>
		void SetLedColor(eLedColor color);
	}
}