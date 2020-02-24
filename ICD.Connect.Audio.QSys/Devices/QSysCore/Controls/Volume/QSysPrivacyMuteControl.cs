using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls.Volume
{
	public sealed class QSysPrivacyMuteControl : AbstractVolumeDeviceControl<QSysCoreDevice>, IQSysKrangControl
	{
		private readonly string m_Name;

		[CanBeNull]
		private readonly BooleanNamedControl m_MuteControl;

		#region Properties

		public override string Name { get { return string.IsNullOrEmpty(m_Name) ? base.Name : m_Name; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 0; } }

		/// <summary>
		/// Gets the current volume, in string representation (e.g. percentage, decibels).
		/// </summary>
		public override string VolumeString { get { return null; } }

		#endregion

		/// <summary>
		/// Constructor used to load control from xml
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public QSysPrivacyMuteControl(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id)
		{
			m_Name = friendlyName;

			string muteName = XmlUtils.TryReadChildElementContentAsString(xml, "MuteControlName");

			// Load mute control
			m_MuteControl = context.LazyLoadNamedControl<BooleanNamedControl>(muteName);

			// Supported features
			if (m_MuteControl != null)
			{
				SupportedVolumeFeatures |= eVolumeFeatures.Mute |
				                           eVolumeFeatures.MuteAssignment |
				                           eVolumeFeatures.MuteFeedback;
			}

			Subscribe(m_MuteControl);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
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
			throw new NotSupportedException();
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			throw new NotSupportedException();
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

		#region Mute Control Callbacks

		/// <summary>
		/// Subscribe to the mute control events.
		/// </summary>
		/// <param name="muteControl"></param>
		private void Subscribe(BooleanNamedControl muteControl)
		{
			if (muteControl == null)
				return;

			muteControl.OnValueUpdated += MuteControlOnValueUpdated;
		}

		/// <summary>
		/// Unsubscribe from the mute control events.
		/// </summary>
		/// <param name="muteControl"></param>
		private void Unsubscribe(BooleanNamedControl muteControl)
		{
			if (muteControl == null)
				return;

			muteControl.OnValueUpdated -= MuteControlOnValueUpdated;
		}

		/// <summary>
		/// Called when the mute control state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void MuteControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			IsMuted = BooleanNamedControl.GetValueAsBool(args.ValueRaw);
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

			yield return m_MuteControl;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
