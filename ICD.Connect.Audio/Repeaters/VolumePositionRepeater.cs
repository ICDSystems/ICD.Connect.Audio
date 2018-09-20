using System;
using ICD.Common.Utils;
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
		/// Gets/sets the initial position volume increment amount.
		/// </summary>
		public float InitialIncrement { get; set; }

		/// <summary>
		/// Gets/sets the subsequent position volume increment amounts.
		/// </summary>
		public float RepeatIncrement { get; set; }

		private bool m_PositionHold;
		private float m_PositionDelta;
		private bool m_StartHolding;

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
		/// Begin ramping the volume position in increments of the given size.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumeUpHoldPosition(float increment)
		{
			m_PositionHold = true;
			m_PositionDelta = increment;
			m_StartHolding = true;

			VolumeHold(true);
		}

		/// <summary>
		/// Begin ramping the volume position in decrements of the given size.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumeDownHoldPosition(float decrement)
		{
			m_PositionHold = true;
			m_PositionDelta = decrement;
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
				m_PositionHold = false;
				m_PositionDelta = 0.0f;
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
			if (m_PositionHold)
				IncrementPosition(m_PositionDelta);
			else if (InitialIncrement > 0.0f)
				IncrementPosition(InitialIncrement);
			else
				IncrementPosition();
		}

		/// <summary>
		/// Performs the increment for the repeats
		/// </summary>
		protected override void IncrementVolumeSubsequent()
		{
			if (m_PositionHold)
				IncrementPosition(m_PositionDelta);
			else if (RepeatIncrement > 0.0f)
				IncrementPosition(RepeatIncrement);
			else
				IncrementPosition();
		}

		/// <summary>
		/// Adjusts the device volume by the specified increment.
		/// Applies Up/Down offset based on Up property value.
		/// </summary>
		/// <param name="increment"></param>
		private void IncrementPosition(float increment)
		{
			if (m_Control == null)
				throw new InvalidOperationException("Can't increment volume without control set");

			float delta = Up ? increment : -1 * increment;
			float newPosition = MathUtils.Clamp(m_Control.VolumePosition + delta, 0.0f, 1.0f);

			m_Control.SetVolumePosition(newPosition);
		}

		/// <summary>
		/// Adjusts the device volume using the default Increment/Decremnet methods.
		/// Determines Increment/Decrement bad on Up property value.
		/// </summary>
		private void IncrementPosition()
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
