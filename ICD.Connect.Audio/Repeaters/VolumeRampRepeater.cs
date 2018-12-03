using System;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public sealed class VolumeRampRepeater : AbstactVolumeRepeater
	{
		private IVolumeRampDeviceControl m_Control;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="beforeRepeat">The delay before the second increment</param>
		/// <param name="betweenRepeat">The delay between each subsequent repeat</param>
		public VolumeRampRepeater(long beforeRepeat, long betweenRepeat)
			: base(beforeRepeat, betweenRepeat)
		{
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~VolumeRampRepeater()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the control.
		/// </summary>
		/// <param name="control"></param>
		public void SetControl(IVolumeRampDeviceControl control)
		{
			m_Control = control;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Callback for the initial ramp increment.
		/// </summary>
		protected override void IncrementVolumeInitial()
		{
			IncrementVolume();
		}

		/// <summary>
		/// Callback for each subsequent ramp increment.
		/// </summary>
		protected override void IncrementVolumeSubsequent()
		{
			IncrementVolume();
		}

		/// <summary>
		/// Adjusts the device volume.
		/// </summary>
		private void IncrementVolume()
		{
			if (m_Control == null)
				throw new InvalidOperationException("Can't increment volume without control set");

			if (Up)
				m_Control.VolumeIncrement();
			else
				m_Control.VolumeDecrement();

		}

		#endregion
	}
}
