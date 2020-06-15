using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Volume;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Audio.Proxies.Controls.Volume
{
	public sealed class ProxyVolumeDeviceControl : AbstractProxyDeviceControl, IVolumeDeviceControl
	{
		private const double TOLERANCE = 0.001;

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
		/// Gets the current volume, in string representation
		/// </summary>
		public string VolumeString { get; private set; }

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures
		{
			get { return m_SupportedVolumeFeatures; }
			private set
			{
				if (value == m_SupportedVolumeFeatures)
					return;

				m_SupportedVolumeFeatures = value;

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
			private set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				OnIsMutedChanged.Raise(this, new VolumeControlIsMutedChangedApiEventArgs(value));
			}
		}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public float VolumeLevel
		{
			get { return m_VolumeLevel; }
			private set
			{
				if (Math.Abs(m_VolumeLevel - value) < TOLERANCE)
					return;

				m_VolumeLevel = value;

				OnVolumeChanged.Raise(this,
				                      new VolumeControlVolumeChangedApiEventArgs(m_VolumeLevel, this.GetVolumePercent(),
				                                                                 VolumeString));
			}
		}

		/// <summary>
		/// VolumeLevelMaxRange is the best max volume we have for the control
		/// either the Max from the control or the absolute max for the control
		/// </summary>
		public float VolumeLevelMax { get; private set; }

		/// <summary>
		/// VolumeLevelMinRange is the best min volume we have for the control
		/// either the Min from the control or the absolute min for the control
		/// </summary>
		public float VolumeLevelMin { get; private set; }

		#endregion
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyVolumeDeviceControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetIsMuted(bool mute)
		{
			CallMethod(VolumeDeviceControlApi.METHOD_SET_IS_MUTED, mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void ToggleIsMuted()
		{
			CallMethod(VolumeDeviceControlApi.METHOD_TOGGLE_IS_MUTED);
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public void SetVolumeLevel(float level)
		{
			CallMethod(VolumeDeviceControlApi.METHOD_SET_VOLUME_LEVEL, level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeIncrement()
		{
			CallMethod(VolumeDeviceControlApi.METHOD_VOLUME_INCREMENT);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public void VolumeDecrement()
		{
			CallMethod(VolumeDeviceControlApi.METHOD_VOLUME_DECREMENT);
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public void VolumeRamp(bool increment, long timeout)
		{

			CallMethod(VolumeDeviceControlApi.METHOD_VOLUME_RAMP, increment, timeout);
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeRampStop()
		{
			CallMethod(VolumeDeviceControlApi.METHOD_VOLUME_RAMP_STOP);
		}

		#endregion

		#region API

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
			                 .SubscribeEvent(VolumeDeviceControlApi.EVENT_VOLUME_CHANGED)
							 .SubscribeEvent(VolumeDeviceControlApi.EVENT_IS_MUTED_CHANGED)
							 .SubscribeEvent(VolumeDeviceControlApi.EVENT_SUPPORTED_VOLUME_FEATURES_CHANGED)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_SUPPORTED_VOLUME_FEATURES)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MAX)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MIN)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_VOLUME_STRING)
							 .GetProperty(VolumeDeviceControlApi.PROPERTY_IS_MUTED)
			                 .Complete();
		}

		/// <summary>
		/// Updates the proxy with event feedback info.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseEvent(string name, ApiResult result)
		{
			base.ParseEvent(name, result);

			switch (name)
			{
				case VolumeDeviceControlApi.EVENT_VOLUME_CHANGED:
					HandleVolumeChangeEvent(result.GetValue<VolumeChangeState>());
					break;
				case VolumeDeviceControlApi.EVENT_IS_MUTED_CHANGED:
					IsMuted = result.GetValue<bool>();
					break;
				case VolumeDeviceControlApi.EVENT_SUPPORTED_VOLUME_FEATURES_CHANGED:
					SupportedVolumeFeatures = result.GetValue<eVolumeFeatures>();
					break;
			}
		}

		/// <summary>
		/// Updates the proxy with a property result.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseProperty(string name, ApiResult result)
		{
			base.ParseProperty(name, result);

			switch (name)
			{
				case VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MAX:
					VolumeLevelMax = result.GetValue<float>();
					break;
				case VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL_MIN:
					VolumeLevelMin = result.GetValue<float>();
					break;
				case VolumeDeviceControlApi.PROPERTY_VOLUME_LEVEL:
					VolumeLevel = result.GetValue<float>();
					break;
				case VolumeDeviceControlApi.PROPERTY_VOLUME_STRING:
					VolumeString = result.GetValue<string>();
					break;
				case VolumeDeviceControlApi.PROPERTY_SUPPORTED_VOLUME_FEATURES:
					SupportedVolumeFeatures = result.GetValue<eVolumeFeatures>();
					break;
				case VolumeDeviceControlApi.PROPERTY_IS_MUTED:
					IsMuted = result.GetValue<bool>();
					break;
			}
		}

		#endregion

		#region Private Methods

		private void HandleVolumeChangeEvent(VolumeChangeState volumeState)
		{
			// Update volume string before level - level raises the change event
			VolumeString = volumeState.VolumeString;
			VolumeLevel = volumeState.VolumeLevel;
		}

		#endregion

		#region Console

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