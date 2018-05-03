using System;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.Controls
{
	public abstract class AbstractVolumeRawLevelDeviceControl<T> : AbstractVolumeLevelDeviceControl<T>, IVolumeRawLevelDeviceControl where T : IDeviceBase
	{
		#region Abstract Properties

		/// <summary>
		/// Absolute Minimum the raw volume can be
		/// Used as a last resort for position caculation
		/// </summary>
		protected abstract float VolumeRawMinAbsolute { get; }

		/// <summary>
		/// Absolute Maximum the raw volume can be
		/// Used as a last resport for position caculation
		/// </summary>
		protected abstract float VolumeRawMaxAbsolute { get; }
		#endregion

		#region Properties

		/// <summary>
		/// VolumeRawMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeRawMinRange
		{
			get
			{
				return VolumeRawMin == null ? VolumeRawMinAbsolute : Math.Max(VolumeRawMinAbsolute, (float)VolumeRawMin);
			}
		}

		/// <summary>
		/// VolumeRawMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeRawMaxRange
		{
			get
			{
				return VolumeRawMax == null ? VolumeRawMaxAbsolute : Math.Min(VolumeRawMaxAbsolute, (float)VolumeRawMax);
			}
		}

		/// <summary>
		/// Gets the position of the volume in the specified range
		/// </summary>
		public override float VolumePosition
		{
			get { return this.ConvertRawToPosition(VolumeRaw); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the volume position in the specified range
		/// </summary>
		/// <param name="position"></param>
		public override void SetVolumePosition(float position)
		{
			SetVolumeRaw(this.ConvertPositionToRaw(position));
		}

		#endregion

		#region Private/Protected Methods

		public virtual void VolumeFeedback(float volumeRaw)
		{
			VolumeFeedback(volumeRaw, this.ConvertRawToPosition(volumeRaw));
		}

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		protected AbstractVolumeRawLevelDeviceControl(T parent, int id) : base(parent, id)
		{
		}
	}
}
