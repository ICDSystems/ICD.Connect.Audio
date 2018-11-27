using System;

namespace ICD.Connect.Audio.Repeaters
{
	public interface IVolumeRepeater : IDisposable
	{
		/// <summary>
		/// Gets/sets amount of time in milliseconds before the initial ramp.
		/// </summary>
		long BeforeRepeat { get; set; }

		/// <summary>
		/// Gets/sets the amount of time in milliseconds between every subsequent ramp.
		/// </summary>
		long BetweenRepeat { get; set; }

		/// <summary>
		/// Begin incrementing the volume.
		/// </summary>
		void VolumeUpHold();

		/// <summary>
		/// Begin decrementing the volume.
		/// </summary>
		void VolumeDownHold();

		/// <summary>
		/// Begin increment/decrement based on bool
		/// </summary>
		/// <param name="up"></param>
		void VolumeHold(bool up);

		/// <summary>
		/// Stops the repeat timer.
		/// </summary>
		void Release();
	}
}
