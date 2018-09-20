using System;
using ICD.Connect.Audio.Controls;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public sealed class VolumeLevelRepeater : AbstactVolumeRepeater
	{
		private IVolumeLevelDeviceControl m_Control;

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
		private bool m_StartHolding;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initialIncrement">The increment when the repeater is first held</param>
		/// <param name="repeatIncrement">The increment for every subsequent repeat</param>
		/// <param name="beforeRepeat">The delay before the second increment</param>
		/// <param name="betweenRepeat">The delay between each subsequent repeat</param>
		public VolumeLevelRepeater(float initialIncrement, float repeatIncrement, long beforeRepeat, long betweenRepeat)
			: base(beforeRepeat, betweenRepeat)
		{
			InitialIncrement = initialIncrement;
			RepeatIncrement = repeatIncrement;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~VolumeLevelRepeater()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the control.
		/// </summary>
		/// <param name="control"></param>
		public void SetControl(IVolumeLevelDeviceControl control)
		{
			m_Control = control;
		}

		/// <summary>
		/// Begin ramping the volume level in increments of the given size.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumeUpHoldLevel(float increment)
		{
			m_LevelHold = true;
			m_LevelDelta = increment;
			m_StartHolding = true;

			VolumeHold(true);
		}

		/// <summary>
		/// Begin ramping the volume level in decrements of the given size.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumeDownHoldLevel(float decrement)
		{
			m_LevelHold = true;
			m_LevelDelta = decrement;
			m_StartHolding = true;

			VolumeHold(false);
		}

		/// <summary>
		/// Stops the repeat timer.
		/// </summary>
		public override void Release()
		{
			if (!m_StartHolding)
			{
				m_LevelHold = false;
				m_LevelDelta = 0.0f;
			}

			m_StartHolding = false;

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
				IncrementVolumeLevel(m_LevelDelta);
			else if (InitialIncrement > 0.0f)
				IncrementVolumeLevel(InitialIncrement);
			else
				IncrementVolumeLevel();
		}

		/// <summary>
		/// Performs the increment for the repeats
		/// </summary>
		protected override void IncrementVolumeSubsequent()
		{
			if (m_LevelHold)
				IncrementVolumeLevel(m_LevelDelta);
			else if (RepeatIncrement > 0.0f)
				IncrementVolumeLevel(RepeatIncrement);
			else
				IncrementVolumeLevel();
		}

		/// <summary>
		/// Adjusts the device volume by the specified increment.
		/// Applies Up/Down offset based on Up property value.
		/// </summary>
		/// <param name="increment"></param>
		private void IncrementVolumeLevel(float increment)
		{
			if (m_Control == null)
				throw new InvalidOperationException("Can't increment volume without control set");

			float delta = Up ? increment : -1 * increment;
			float newVolume = m_Control.ClampToVolumeLevel(m_Control.VolumeLevel + delta);

			m_Control.SetVolumeLevel(newVolume);
		}

		/// <summary>
		/// Adjusts the device volume using the default Increment/Decremnet methods.
		/// Determines Increment/Decrement bad on Up property value.
		/// </summary>
		private void IncrementVolumeLevel()
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
