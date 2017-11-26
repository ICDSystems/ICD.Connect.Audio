using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices.Controls;

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
		private const int VOLUME_MAX = 99;

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

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

		public override void SetRawVolume(float volume)
		{
			int vol = (int)MathUtils.Clamp(volume, VOLUME_MIN, VOLUME_MAX);

			DenonSerialData data = DenonSerialData.Command(MASTER_VOLUME_SET, vol);
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
			switch (response.GetCommand())
			{
				case MUTE_ON:
					IsMuted = true;
					break;

				case MUTE_OFF:
					IsMuted = false;
					break;
			}
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
