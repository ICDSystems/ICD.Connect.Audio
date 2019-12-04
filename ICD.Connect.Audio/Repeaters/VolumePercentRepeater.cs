using System;
using ICD.Common.Utils;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public sealed class VolumePercentRepeater : AbstactVolumeRepeater
	{
		private IVolumePercentDeviceControl m_Control;

		/// <summary>
		/// Gets/sets the initial percent volume increment amount.
		/// </summary>
		public float InitialIncrement { get; set; }

		/// <summary>
		/// Gets/sets the subsequent percent volume increment amounts.
		/// </summary>
		public float RepeatIncrement { get; set; }

		private bool m_PercentHold;
		private float m_PercentDelta;
		private bool m_StartHolding;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initialIncrement">The increment when the repeater is first held</param>
		/// <param name="repeatIncrement">The increment for every subsequent repeat</param>
		/// <param name="beforeRepeat">The delay before the second increment</param>
		/// <param name="betweenRepeat">The delay between each subsequent repeat</param>
		public VolumePercentRepeater(float initialIncrement, float repeatIncrement, long beforeRepeat, long betweenRepeat)
			: base(beforeRepeat, betweenRepeat)
		{
			InitialIncrement = initialIncrement;
			RepeatIncrement = repeatIncrement;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~VolumePercentRepeater()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the control.
		/// </summary>
		/// <param name="control"></param>
		public void SetControl(IVolumePercentDeviceControl control)
		{
			m_Control = control;
		}

		/// <summary>
		/// Begin ramping the volume percent in increments of the given size.
		/// </summary>
		/// <param name="increment"></param>
		public void VolumeUpHoldPercent(float increment)
		{
			m_PercentHold = true;
			m_PercentDelta = increment;
			m_StartHolding = true;

			VolumeHold(true);
		}

		/// <summary>
		/// Begin ramping the volume percent in decrements of the given size.
		/// </summary>
		/// <param name="decrement"></param>
		public void VolumeDownHoldPercent(float decrement)
		{
			m_PercentHold = true;
			m_PercentDelta = decrement;
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
				m_PercentHold = false;
				m_PercentDelta = 0.0f;
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
			if (m_PercentHold)
				IncrementPercent(m_PercentDelta);
			else if (InitialIncrement > 0.0f)
				IncrementPercent(InitialIncrement);
			else
				IncrementPercent();
		}

		/// <summary>
		/// Performs the increment for the repeats
		/// </summary>
		protected override void IncrementVolumeSubsequent()
		{
			if (m_PercentHold)
				IncrementPercent(m_PercentDelta);
			else if (RepeatIncrement > 0.0f)
				IncrementPercent(RepeatIncrement);
			else
				IncrementPercent();
		}

		/// <summary>
		/// Adjusts the device volume by the specified increment.
		/// Applies Up/Down offset based on Up property value.
		/// </summary>
		/// <param name="increment"></param>
		private void IncrementPercent(float increment)
		{
			if (m_Control == null)
				throw new InvalidOperationException("Can't increment volume without control set");

			float delta = Up ? increment : -1 * increment;
			float newPercent = MathUtils.Clamp(m_Control.VolumePercent + delta, 0.0f, 1.0f);

			m_Control.SetVolumePercent(newPercent);
		}

		/// <summary>
		/// Adjusts the device volume using the default Increment/Decremnet methods.
		/// Determines Increment/Decrement bad on Up property value.
		/// </summary>
		private void IncrementPercent()
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
