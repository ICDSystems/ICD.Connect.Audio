using System;
using ICD.Connect.Audio.Controls;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public sealed class VolumePositionRepeater : AbstactVolumeRepeater
	{
		private IVolumePositionDeviceControl m_Control;

		/// <summary>
		/// Gets/sets the initial raw volume increment amount.
		/// </summary>
		public float InitialIncrement { get; set; }

		/// <summary>
		/// Gets/sets the subsequent raw volume increment amounts.
		/// </summary>
		public float RepeatIncrement { get; set; }

		private bool m_LevelHold;
		private float m_LevelDelta;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initialIncrement">The increment when the repeater is first held</param>
		/// <param name="repeatIncrement">The increment for every subsequent repeat</param>
		/// <param name="beforeRepeat">The delay before the second increment</param>
		/// <param name="betweenRepeat">The delay between each subsequent repeat</param>
		public VolumePositionRepeater(float initialIncrement, float repeatIncrement, long beforeRepeat, long betweenRepeat)
			: base(beforeRepeat, betweenRepeat)
		{
			InitialIncrement = initialIncrement;
			RepeatIncrement = repeatIncrement;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~VolumePositionRepeater()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the control.
		/// </summary>
		/// <param name="control"></param>
		public void SetControl(IVolumePositionDeviceControl control)
		{
			m_Control = control;
		}

		/// <summary>
		/// Stops the repeat timer.
		/// </summary>
		public override void Release()
		{
			m_LevelHold = false;
			m_LevelDelta = 0.0f;

			base.Release();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Performs the increment for the initial press
		/// </summary>
		protected override void IncrementVolumeInitial()
		{
			if (m_LevelHold)
				IncrementVolume(m_LevelDelta);
			else if (InitialIncrement > 0.0f)
				IncrementVolume(InitialIncrement);
			else
				IncrementVolume();
		}

		/// <summary>
		/// Performs the increment for the repeats
		/// </summary>
		protected override void IncrementVolumeSubsequent()
		{
			if (m_LevelHold)
				IncrementVolume(m_LevelDelta);
			else if (RepeatIncrement > 0.0f)
				IncrementVolume(RepeatIncrement);
			else
				IncrementVolume();
		}

		/// <summary>
		/// Adjusts the device volume by the specified increment.
		/// Applies Up/Down offset based on Up property value.
		/// </summary>
		/// <param name="increment"></param>
		private void IncrementVolume(float increment)
		{
			if (m_Control == null)
				throw new InvalidOperationException("Can't increment volume without control set");

			float delta = Up ? increment : -1 * increment;
			float newVolume = m_Control.ClampRawVolume(m_Control.VolumeRaw + delta);

			m_Control.SetVolumeRaw(newVolume);
		}

		/// <summary>
		/// Adjusts the device volume using the default Increment/Decremnet methods.
		/// Determines Increment/Decrement bad on Up property value.
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
