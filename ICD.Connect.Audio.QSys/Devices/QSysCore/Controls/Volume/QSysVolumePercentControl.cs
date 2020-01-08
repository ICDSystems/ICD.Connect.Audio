using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Volume
{
	public sealed class QSysVolumePercentControl : AbstractVolumeDeviceControl<QSysCoreDevice>, IQSysKrangControl
	{
		private readonly string m_Name;

		[CanBeNull] private readonly INamedControl m_VolumeControl;

		[CanBeNull] private readonly BooleanNamedControl m_MuteControl;

		#region Properties

		public override string Name { get { return string.IsNullOrEmpty(m_Name) ? base.Name : m_Name; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 1; } }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		public override string VolumeString { get { return m_VolumeControl == null ? base.VolumeString : m_VolumeControl.ValueString; } }

		#endregion

		/// <summary>
		/// Constructor used to load control from xml
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public QSysVolumePercentControl(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id)
		{
			m_Name = friendlyName;

			string volumeName = XmlUtils.TryReadChildElementContentAsString(xml, "VolumeControlName");
			string muteName = XmlUtils.TryReadChildElementContentAsString(xml, "MuteControlName");

			// Load volume/mute controls
			m_VolumeControl = context.LazyLoadNamedControl<NamedControl>(volumeName);
			m_MuteControl = context.LazyLoadNamedControl<BooleanNamedControl>(muteName);

			// Supported features
			if (m_MuteControl != null)
			{
				SupportedVolumeFeatures |= eVolumeFeatures.Mute |
				                           eVolumeFeatures.MuteAssignment |
				                           eVolumeFeatures.MuteFeedback;
			}
			if (m_VolumeControl != null)
			{
				SupportedVolumeFeatures |= eVolumeFeatures.Volume |
				                           eVolumeFeatures.VolumeAssignment |
				                           eVolumeFeatures.VolumeFeedback;
			}

			Subscribe(m_VolumeControl);
			Subscribe(m_MuteControl);
		}

		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(m_VolumeControl);
			Unsubscribe(m_MuteControl);

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			if (m_MuteControl == null)
				throw new NotSupportedException("Unable to set mute state - Mute control is null");

			m_MuteControl.SetValue(mute);
		}

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
			if (m_VolumeControl == null)
				throw new NotSupportedException("Unable to set raw volume - Volume control is null");

			m_VolumeControl.SetValue(level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			if (m_VolumeControl == null)
				throw new NotSupportedException("Unable to increment volume - Volume control is null");

			m_VolumeControl.SetValue(string.Format("+={0}", 0.01f));
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			if (m_VolumeControl == null)
				throw new NotSupportedException("Unable to decrement volume - Volume control is null");

			m_VolumeControl.SetValue(string.Format("-={0}", 0.01f));
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

		#region Volume Control Callbacks

		private void Subscribe(INamedControl volumeControl)
		{
			if (volumeControl == null)
				return;

			volumeControl.OnValueUpdated += VolumeControlOnValueUpdated;
		}

		private void Unsubscribe(INamedControl volumeControl)
		{
			if (volumeControl == null)
				return;

			volumeControl.OnValueUpdated -= VolumeControlOnValueUpdated;
		}

		private void VolumeControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			VolumeLevel = m_VolumeControl == null ? 0 : m_VolumeControl.ValueRaw;
		}

		#endregion

		#region Mute Control Callbacks

		private void Subscribe(BooleanNamedControl muteControl)
		{
			if (muteControl == null)
				return;

			muteControl.OnValueUpdated += MuteControlOnValueUpdated;
		}

		private void Unsubscribe(BooleanNamedControl muteControl)
		{
			if (muteControl == null)
				return;

			muteControl.OnValueUpdated -= MuteControlOnValueUpdated;
		}

		private void MuteControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			IsMuted = BooleanNamedControl.GetValueAsBool(args.ValueRaw);
		}

		#endregion

		#region Console

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return m_VolumeControl;
			yield return m_MuteControl;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
