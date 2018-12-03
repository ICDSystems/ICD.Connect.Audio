using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Mute;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
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

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		private bool m_VolumeIsMuted;
		private float m_VolumeLevel;

		#region Properties

		/// <summary>
		/// Absolute Minimum the raw volume can be
		/// Used as a last resort for position caculation
		/// </summary>
		protected override float VolumeRawMinAbsolute { get { return VOLUME_MIN; } }

		/// <summary>
		/// Absolute Maximum the raw volume can be
		/// Used as a last resport for position caculation
		/// </summary>
		protected override float VolumeRawMaxAbsolute { get { return VOLUME_MAX; } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted
		{
			get { return m_VolumeIsMuted; }
			private set
			{
				if (value == m_VolumeIsMuted)
					return;

				m_VolumeIsMuted = value;

				Log(eSeverity.Informational, "Mute set to {0}", m_VolumeIsMuted);

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_VolumeIsMuted));
			}
		}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public override float VolumeLevel { get { return m_VolumeLevel; } }

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
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeLevel(float volume)
		{
			DenonSerialData data = GetVolumeCommand(volume);
			Parent.SendData(data);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			DenonSerialData data = DenonSerialData.Command(mute ? MUTE_ON : MUTE_OFF);
			Parent.SendData(data);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			SetVolumeMute(!VolumeIsMuted);
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
					VolumeIsMuted = true;
					return;

				case MUTE_OFF:
					VolumeIsMuted = false;
					return;
			}

			if (data == MASTER_VOLUME)
			{
				float volume = GetVolumeFromResponse(response.GetValue());
				UpdateVolumeLevel(volume);
			}
		}

		private void UpdateVolumeLevel(float volume)
		{
			if (Math.Abs(volume - m_VolumeLevel) < 0.001f)
				return;

			m_VolumeLevel = volume;

			Log(eSeverity.Informational, "Volume level set to {0}", StringUtils.NiceName(m_VolumeLevel));

			VolumeFeedback(m_VolumeLevel);
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			Parent.SendData(DenonSerialData.Request(MASTER_VOLUME));
			Parent.SendData(DenonSerialData.Request(MUTE));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in VolumeMuteBasicDeviceControlConsole.GetConsoleNodes(this))
				yield return node;

			foreach (IConsoleNodeBase node in VolumeMuteDeviceControlConsole.GetConsoleNodes(this))
				yield return node;

			foreach (IConsoleNodeBase node in VolumeMuteFeedbackDeviceControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Wrokaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			VolumeMuteBasicDeviceControlConsole.BuildConsoleStatus(this, addRow);
			VolumeMuteDeviceControlConsole.BuildConsoleStatus(this, addRow);
			VolumeMuteFeedbackDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteBasicDeviceControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteDeviceControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteFeedbackDeviceControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
