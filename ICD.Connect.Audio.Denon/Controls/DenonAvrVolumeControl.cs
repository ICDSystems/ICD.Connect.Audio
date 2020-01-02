using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.Denon.Devices;

namespace ICD.Connect.Audio.Denon.Controls
{
    public sealed class DenonAvrVolumeControl : AbstractVolumeDeviceControl<DenonAvrDevice>
	{
		private const string MASTER_VOLUME = "MV";
		private const string MASTER_VOLUME_UP = MASTER_VOLUME + "UP";
		private const string MASTER_VOLUME_DOWN = MASTER_VOLUME + "DOWN";
		private const string MASTER_VOLUME_SET = MASTER_VOLUME + "{0:D2}";

		private const string MUTE = "MU";
		private const string MUTE_ON = MUTE + "ON";
		private const string MUTE_OFF = MUTE + "OFF";

		private const int VOLUME_MIN = 0;
		private const int VOLUME_MAX = 98;

		#region Properties

	    /// <summary>
	    /// Returns the features that are supported by this volume control.
	    /// </summary>
	    public override eVolumeFeatures SupportedVolumeFeatures
	    {
		    get
		    {
			    return eVolumeFeatures.Mute |
			           eVolumeFeatures.MuteAssignment |
			           eVolumeFeatures.MuteFeedback |
			           eVolumeFeatures.Volume |
			           eVolumeFeatures.VolumeAssignment |
			           eVolumeFeatures.VolumeFeedback;
		    }
	    }

	    /// <summary>
	    /// Gets the minimum supported volume level.
	    /// </summary>
		public override float VolumeLevelMin { get { return VOLUME_MIN; } }

	    /// <summary>
	    /// Gets the maximum supported volume level.
	    /// </summary>
		public override float VolumeLevelMax { get { return VOLUME_MAX; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DenonAvrVolumeControl(DenonAvrDevice parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

	    /// <summary>
	    /// Toggles the current mute state.
	    /// </summary>
	    public override void ToggleIsMuted()
	    {
			SetIsMuted(!IsMuted);
	    }

	    /// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			DenonSerialData data = GetVolumeCommand(level);
			Parent.SendData(data);
		}

	    /// <summary>
	    /// Raises the volume one time
	    /// Amount of the change varies between implementations - typically "1" raw unit
	    /// </summary>
	    public override void VolumeIncrement()
	    {
			DenonSerialData data = DenonSerialData.Command(MASTER_VOLUME_UP);
			Parent.SendData(data);
	    }

	    /// <summary>
	    /// Lowers the volume one time
	    /// Amount of the change varies between implementations - typically "1" raw unit
	    /// </summary>
	    public override void VolumeDecrement()
	    {
			DenonSerialData data = DenonSerialData.Command(MASTER_VOLUME_DOWN);
			Parent.SendData(data);
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

	    /// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			DenonSerialData data = DenonSerialData.Command(mute ? MUTE_ON : MUTE_OFF);
			Parent.SendData(data);
		}

	    #endregion

		#region Private Methods

		/// <summary>
		/// Builds a volume command for the given volume level.
		/// </summary>
		/// <param name="volume"></param>
		/// <returns></returns>
		private DenonSerialData GetVolumeCommand(float volume)
		{
			volume = MathUtils.Clamp(volume, VOLUME_MIN, VOLUME_MAX);

			// Volume commands are 2 digits for whole numbers, 3 digits for "half steps" e.g. 45.5 = 455
			volume = volume * 10;
			int vol = (int)Math.Round(volume / 5.0) * 5;
			if (vol % 10 == 0)
				vol /= 10;

			return DenonSerialData.Command(MASTER_VOLUME_SET, vol);
		}

		/// <summary>
		/// Gets the volume from the given response string.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static float GetVolumeFromResponse(string data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			float value = float.Parse(data);
			if (value > VOLUME_MAX)
				value /= 10.0f;

			return value;
		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(DenonAvrDevice parent)
		{
			base.Subscribe(parent);

			parent.OnInitializedChanged += ParentOnInitializedChanged;
			parent.OnDataReceived += ParentOnDataReceived;
		}

		protected override void Unsubscribe(DenonAvrDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnInitializedChanged -= ParentOnInitializedChanged;
			parent.OnDataReceived -= ParentOnDataReceived;
		}

		private void ParentOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			string data = response.GetCommand();

			switch (data)
			{
				case MUTE_ON:
					IsMuted = true;
					break;

				case MUTE_OFF:
					IsMuted = false;
					break;

				case MASTER_VOLUME:
					VolumeLevel = GetVolumeFromResponse(response.GetValue());
					break;
			}	
		}

		private void ParentOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			Parent.SendData(DenonSerialData.Request(MASTER_VOLUME));
			Parent.SendData(DenonSerialData.Request(MUTE));
		}

		#endregion
	}
}
