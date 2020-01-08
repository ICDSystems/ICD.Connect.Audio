using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Audio.Utils;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Controls.Volume
{
	public abstract class AbstractVolumeDeviceControl<T> : AbstractDeviceControl<T>, IVolumeDeviceControl
		where T : IDeviceBase
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// Will not raise if mute feedback is not supported.
		/// </summary>
		public event EventHandler<VolumeControlIsMutedChangedApiEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the volume changes.
		/// Will not raise if volume feedback is not supported.
		/// </summary>
		public event EventHandler<VolumeControlVolumeChangedApiEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the supported volume features change.
		/// </summary>
		public event EventHandler<VolumeControlSupportedVolumeFeaturesChangedApiEventArgs> OnSupportedVolumeFeaturesChanged;

		private bool m_IsMuted;
		private float m_VolumeLevel;
		private eVolumeFeatures m_SupportedVolumeFeatures;

		#region Properties

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures
		{
			get { return m_SupportedVolumeFeatures; }
			protected set
			{
				if (value == m_SupportedVolumeFeatures)
					return;

				m_SupportedVolumeFeatures = value;

				Log(eSeverity.Informational, "Supported volume features changed to {0}", m_IsMuted);

				OnSupportedVolumeFeaturesChanged.Raise(this,
				                                       new VolumeControlSupportedVolumeFeaturesChangedApiEventArgs(
					                                       m_SupportedVolumeFeatures));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// Will return false if mute feedback is not supported.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			protected set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Log(eSeverity.Informational, "IsMuted changed to {0}", m_IsMuted);

				OnIsMutedChanged.Raise(this, new VolumeControlIsMutedChangedApiEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// Gets the current volume in the range VolumeLevelMin to VolumeLevelMax.
		/// </summary>
		public float VolumeLevel
		{
			get { return m_VolumeLevel; }
			protected set
			{
				if (Math.Abs(value - m_VolumeLevel) < 0.01f)
					return;

				m_VolumeLevel = value;

				Log(eSeverity.Informational, "Volume changed: Level={0:F2} Percent={1:P2} Name={2}",
				    m_VolumeLevel, this.GetVolumePercent(), VolumeString);

				OnVolumeChanged.Raise(this,
				                      new VolumeControlVolumeChangedApiEventArgs(m_VolumeLevel, this.GetVolumePercent(),
				                                                                 VolumeString));
			}
		}

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public abstract float VolumeLevelMin { get; }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public abstract float VolumeLevelMax { get; }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		public virtual string VolumeString { get { return VolumeUtils.ToString(this.GetVolumePercent(), eVolumeRepresentation.Percent); } }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public abstract void SetIsMuted(bool mute);

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public abstract void ToggleIsMuted();

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		public abstract void SetVolumeLevel(float level);

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public abstract void VolumeIncrement();

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public abstract void VolumeDecrement();

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public abstract void VolumeRamp(bool increment, long timeout);

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public abstract void VolumeRampStop();

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractVolumeDeviceControl(T parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnIsMutedChanged = null;
			OnVolumeChanged = null;
			OnSupportedVolumeFeaturesChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in VolumeDeviceControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
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

			VolumeDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeDeviceControlConsole.GetConsoleCommands(this))
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
