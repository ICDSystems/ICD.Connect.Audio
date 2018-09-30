using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.Denon.Devices;

namespace ICD.Connect.Audio.Denon.Controls
{
    public sealed class DenonAvrVolumeControl : AbstractVolumeLevelDeviceControl<DenonAvrDevice>, IVolumeMuteFeedbackDeviceControl
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

		public override float RawVolumeMin { get { return VOLUME_MIN; } }

		public override float RawVolumeMax { get { return VOLUME_MAX; } }

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

		public override void SetRawVolume(float volume)
		{
			DenonSerialData data = GetVolumeCommand(volume);
			Parent.SendData(data);
		}

		public override void SetMute(bool mute)
		{
			DenonSerialData data = DenonSerialData.Command(mute ? MUTE_ON : MUTE_OFF);
			Parent.SendData(data);
		}

		public override void RawVolumeIncrement()
		{
			DenonSerialData data = DenonSerialData.Command(MASTER_VOLUME_UP);
			Parent.SendData(data);
		}

		public override void RawVolumeDecrement()
		{
			DenonSerialData data = DenonSerialData.Command(MASTER_VOLUME_DOWN);
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
		private float GetVolumeFromResponse(string data)
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

		private void Subscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
			parent.OnDataReceived += ParentOnOnDataReceived;
		}

		private void Unsubscribe(DenonAvrDevice parent)
		{
			parent.OnInitializedChanged -= ParentOnOnInitializedChanged;
			parent.OnDataReceived -= ParentOnOnDataReceived;
		}

		private void ParentOnOnDataReceived(DenonAvrDevice device, DenonSerialData response)
		{
			string data = response.GetCommand();

			switch (data)
			{
				case MUTE_ON:
					IsMuted = true;
					return;

				case MUTE_OFF:
					IsMuted = false;
					return;
			}

			if (data == MASTER_VOLUME)
				RawVolume = GetVolumeFromResponse(response.GetValue());
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			Parent.SendData(DenonSerialData.Request(MASTER_VOLUME));
			Parent.SendData(DenonSerialData.Request(MUTE));
		}

		#endregion
	}
}
