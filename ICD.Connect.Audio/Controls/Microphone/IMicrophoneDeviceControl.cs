using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Controls.Microphone
{
	public interface IMicrophoneDeviceControl : IVolumeDeviceControl
	{
		/// <summary>
		/// Raised when the phantom power state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPhantomPowerStateChanged;

		/// <summary>
		/// Raised when the gain level changes.
		/// </summary>
		event EventHandler<FloatEventArgs> OnAnalogGainLevelChanged;

		#region Properties

		/// <summary>
		/// Gets the phantom power state.
		/// </summary>
		bool PhantomPower { get; }

		/// <summary>
		/// Gets the gain level.
		/// </summary>
		float AnalogGainLevel { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the gain level.
		/// </summary>
		/// <param name="level"></param>
		void SetAnalogGainLevel(float level);

		/// <summary>
		/// Sets the phantom power state.
		/// </summary>
		/// <param name="power"></param>
		void SetPhantomPower(bool power);

		#endregion
	}
}
