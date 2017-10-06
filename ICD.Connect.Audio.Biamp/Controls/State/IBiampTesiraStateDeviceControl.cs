using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.State
{
	public interface IBiampTesiraStateDeviceControl : IBiampTesiraDeviceControl
	{
		/// <summary>
		/// Raised when the state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnStateChanged;

		/// <summary>
		/// Gets the state of the control.
		/// </summary>
		bool State { get; }

		/// <summary>
		/// Sets the state of the control.
		/// </summary>
		/// <param name="state"></param>
		void SetState(bool state);
	}
}
