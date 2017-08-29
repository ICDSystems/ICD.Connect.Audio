using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Biamp.Controls.Volume
{
	public sealed class BiampTesiraVolumeDeviceControl : AbstractVolumeDeviceControl<BiampTesiraDevice>
	{
		private readonly IVolumeAttributeInterface m_VolumeInterface;
		private readonly string m_Name;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// The min volume.
		/// </summary>
		public override float RawVolumeMin { get { return m_VolumeInterface.AttributeMinLevel; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		public override float RawVolumeMax { get { return m_VolumeInterface.AttributeMaxLevel; } }
		
		/// <summary>
		/// The volume the control is set to when the device comes online.
		/// </summary>
		public override float? RawVolumeDefault { get; set; }

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
		public override void SetRawVolume(float volume)
		{
			m_VolumeInterface.SetLevel(volume);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetMute(bool mute)
		{
			m_VolumeInterface.SetMute(mute);
		}

		/// <summary>
		/// Increments the raw volume once.
		/// </summary>
		public override void RawVolumeIncrement()
		{
			m_VolumeInterface.IncrementLevel();
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void RawVolumeDecrement()
		{
			m_VolumeInterface.DecrementLevel();
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
			IsMuted = args.Data;
		}

		private void VolumeInterfaceOnLevelChanged(object sender, FloatEventArgs args)
		{
			RawVolume = args.Data;
		}

		#endregion
	}
}
