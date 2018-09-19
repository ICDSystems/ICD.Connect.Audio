using ICD.Common.Utils.Timers;

namespace ICD.Connect.Audio.Repeaters
{
	/// <summary>
	/// VolumeRepeater allows for a virtual "button" to be held, raising a callback for
	/// every repeat interval.
	/// </summary>
	public abstract class AbstactVolumeRepeater : IVolumeRepeater
	{
		private readonly SafeTimer m_RepeatTimer;

		protected bool Up { get; private set; }

		/// <summary>
		/// Gets/sets amount of time in milliseconds before the initial ramp.
		/// </summary>
		public long BeforeRepeat { get; set; }

		/// <summary>
		/// Gets/sets the amount of time in milliseconds between every subsequent ramp.
		/// </summary>
		public long BetweenRepeat { get; set; }

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="beforeRepeat">The delay before the second increment</param>
		/// <param name="betweenRepeat">The delay between each subsequent repeat</param>
		protected AbstactVolumeRepeater(long beforeRepeat, long betweenRepeat)
		{
			m_RepeatTimer = SafeTimer.Stopped(IncrementVolumeSubsequent);

			BeforeRepeat = beforeRepeat;
			BetweenRepeat = betweenRepeat;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~AbstactVolumeRepeater()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			m_RepeatTimer.Dispose();
		}

		/// <summary>
		/// Begin incrementing the volume.
		/// </summary>
		public void VolumeUpHold()
		{
			Release();
			BeginIncrement(true);
		}

		/// <summary>
		/// Begin decrementing the volume.
		/// </summary>
		public void VolumeDownHold()
		{
			Release();
			BeginIncrement(false);
		}

		/// <summary>
		/// Begin increment/decrement based on bool
		/// </summary>
		/// <param name="up"></param>
		public void VolumeHold(bool up)
		{
			Release();
			BeginIncrement(up);
		}

		/// <summary>
		/// Stops the repeat timer.
		/// </summary>
		public void Release()
		{
			m_RepeatTimer.Stop();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Applies the initial increment and resets the timer.
		/// </summary>
		/// <param name="up"></param>
		private void BeginIncrement(bool up)
		{
			Up = up;

			IncrementVolumeInitial();

			m_RepeatTimer.Reset(BeforeRepeat, BetweenRepeat);
		}

		/// <summary>
		/// Callback for the initial ramp increment.
		/// </summary>
		protected abstract void IncrementVolumeInitial();

		/// <summary>
		/// Callback for each subsequent ramp increment.
		/// </summary>
		protected abstract void IncrementVolumeSubsequent();

		#endregion
	}
}
