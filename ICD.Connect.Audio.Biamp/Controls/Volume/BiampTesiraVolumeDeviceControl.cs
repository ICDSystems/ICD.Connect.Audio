using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Biamp.Controls.Volume
{
	public sealed class BiampTesiraVolumeDeviceControl : AbstractVolumeDeviceControl<BiampTesiraDevice>
	{
		private readonly string m_Name;
		private readonly IVolumeAttributeInterface m_VolumeInterface;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return Math.Max(BiampTesiraDevice.TESIRA_LEVEL_MINIMUM, m_VolumeInterface.MinLevel); } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return Math.Min(BiampTesiraDevice.TESIRA_LEVEL_MAXIMUM, m_VolumeInterface.MaxLevel); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="volumeInterface"></param>
		public BiampTesiraVolumeDeviceControl(int id, Guid uuid, string name, IVolumeAttributeInterface volumeInterface)
			: base(volumeInterface.Device, id, uuid)
		{
			m_Name = name;
			m_VolumeInterface = volumeInterface;

			SupportedVolumeFeatures = eVolumeFeatures.Mute |
			                          eVolumeFeatures.MuteAssignment |
			                          eVolumeFeatures.MuteFeedback |
			                          eVolumeFeatures.Volume |
			                          eVolumeFeatures.VolumeAssignment |
			                          eVolumeFeatures.VolumeFeedback;

			Subscribe(m_VolumeInterface);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_VolumeInterface);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			m_VolumeInterface.SetMute(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			m_VolumeInterface.SetMute(!m_VolumeInterface.Mute);
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			m_VolumeInterface.SetLevel(level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			m_VolumeInterface.IncrementLevel();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			m_VolumeInterface.DecrementLevel();
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Volume Interface Callbacks

		private void Subscribe(IVolumeAttributeInterface volumeInterface)
		{
			volumeInterface.OnLevelChanged += VolumeInterfaceOnLevelChanged;
			volumeInterface.OnMuteChanged += VolumeInterfaceOnMuteChanged;
		}

		private void Unsubscribe(IVolumeAttributeInterface volumeInterface)
		{
			volumeInterface.OnLevelChanged -= VolumeInterfaceOnLevelChanged;
			volumeInterface.OnMuteChanged -= VolumeInterfaceOnMuteChanged;
		}

		private void VolumeInterfaceOnMuteChanged(object sender, BoolEventArgs args)
		{
			IsMuted = m_VolumeInterface.Mute;
		}

		private void VolumeInterfaceOnLevelChanged(object sender, FloatEventArgs args)
		{
			VolumeLevel = m_VolumeInterface.Level;
		}

		#endregion
	}
}
