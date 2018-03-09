using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Biamp.Controls.Volume
{
	public sealed class BiampTesiraVolumeDeviceControl : AbstractVolumeRawLevelDeviceControl<BiampTesiraDevice>, IBiampTesiraDeviceControl, IVolumeMuteFeedbackDeviceControl
	{
		private readonly string m_Name;
		private readonly IVolumeAttributeInterface m_VolumeInterface;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		protected override float VolumeRawMinAbsolute { get { return BiampTesiraDevice.TESIRA_LEVEL_MINIMUM; } }

		protected override float VolumeRawMaxAbsolute {  get { return BiampTesiraDevice.TESIRA_LEVEL_MAXIMUM; } }

		/// <summary>
		/// The min volume.
		/// </summary>
		public override float? VolumeRawMin { get { return m_VolumeInterface.MinLevel; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		public override float? VolumeRawMax { get { return m_VolumeInterface.MaxLevel; } }

		public override float VolumeRaw { get { return m_VolumeInterface.Level; } }

		public bool VolumeIsMuted {  get { return m_VolumeInterface.Mute; } }

		#endregion

		#region Events

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="volumeInterface"></param>
		public BiampTesiraVolumeDeviceControl(int id, string name, IVolumeAttributeInterface volumeInterface)
			: base(volumeInterface.Device, id)
		{
			m_Name = name;
			m_VolumeInterface = volumeInterface;

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
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeRaw(float volume)
		{
			m_VolumeInterface.SetLevel(volume);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			m_VolumeInterface.SetMute(mute);
		}

		public void VolumeMuteToggle()
		{
			SetVolumeMute(!VolumeIsMuted);
		}

		/// <summary>
		/// Increments the raw volume by the given unit
		/// </summary>
		/// <param name="incrementValue"></param>
		public override void VolumeLevelIncrement(float incrementValue)
		{
			m_VolumeInterface.IncrementLevel(incrementValue);
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void VolumeLevelDecrement(float decrementValue)
		{
			m_VolumeInterface.DecrementLevel(decrementValue);
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
			OnMuteStateChanged.Raise(this, args);
		}

		private void VolumeInterfaceOnLevelChanged(object sender, FloatEventArgs args)
		{
			VolumeFeedback(args.Data);
		}

		#endregion
	}
}
