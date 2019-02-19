using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.Repeaters;

namespace ICD.Connect.Audio.QSys.Controls.Volume
{
	public sealed class QSysVolumePositionControl : AbstractVolumePositionDeviceControl<QSysCoreDevice>,
	                                                IVolumeMuteFeedbackDeviceControl, IQSysKrangControl
	{
		#region Events

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		private readonly string m_Name;

		[CanBeNull] private readonly INamedControl m_VolumeControl;

		[CanBeNull] private readonly BooleanNamedControl m_MuteControl;

		#region Properties

		public override string Name { get { return m_Name; } }

		public float VolumeRaw { get { return m_VolumeControl == null ? 0 : m_VolumeControl.ValueRaw; } }

		public override float VolumePosition { get { return m_VolumeControl == null ? 0 : m_VolumeControl.ValuePosition; } }

		public override string VolumeString { get { return m_VolumeControl == null ? null : m_VolumeControl.ValueString; } }

		public bool VolumeIsMuted { get { return m_MuteControl != null && m_MuteControl.ValueBool; } }

		#endregion

		/// <summary>
		/// Constructor used to load control from xml
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public QSysVolumePositionControl(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id)
		{
			m_Name = friendlyName;

			string volumeName = XmlUtils.TryReadChildElementContentAsString(xml, "VolumeControlName");
			string muteName = XmlUtils.TryReadChildElementContentAsString(xml, "MuteControlName");
			float? incrementValue = XmlUtils.TryReadChildElementContentAsFloat(xml, "IncrementValue");
			int? repeatBeforeTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBeforeTime");
			int? repeatBetweenTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBetweenTime");

			// Load volume/mute controls
			m_VolumeControl = context.LazyLoadNamedControl(volumeName, typeof(NamedControl)) as NamedControl;
			m_MuteControl = context.LazyLoadNamedControl(muteName, typeof(BooleanNamedControl)) as BooleanNamedControl;

			VolumePositionRepeater positionRepeater = VolumeRepeater as VolumePositionRepeater;

			if (incrementValue != null && positionRepeater != null)
			{
				positionRepeater.InitialIncrement = (float)incrementValue;
				positionRepeater.RepeatIncrement = (float)incrementValue;
			}

			if (repeatBeforeTime != null)
				RepeatBeforeTime = (int)repeatBeforeTime;
			if (repeatBetweenTime != null)
				RepeatBetweenTime = (int)repeatBetweenTime;

			Subscribe();
		}

		#region Methods

		public void SetVolumeRaw(float volume)
		{
			if (m_VolumeControl == null)
			{
				Log(eSeverity.Error, "Unable to set raw volume - Volume control is null");
				return;
			}

			m_VolumeControl.SetValue(volume);
		}

		public override void SetVolumePosition(float position)
		{
			if (m_VolumeControl == null)
			{
				Log(eSeverity.Error, "Unable to set volume position - Volume control is null");
				return;
			}

			m_VolumeControl.SetPosition(position);
		}

		public void VolumeLevelIncrement(float incrementValue)
		{
			if (m_VolumeControl == null)
			{
				Log(eSeverity.Error, "Unable to increment volume - Volume control is null");
				return;
			}

			m_VolumeControl.SetValue(string.Format("+={0}", incrementValue));
		}

		public void VolumeLevelDecrement(float decrementValue)
		{
			if (m_VolumeControl == null)
			{
				Log(eSeverity.Error, "Unable to decrement volume - Volume control is null");
				return;
			}

			m_VolumeControl.SetValue(string.Format("-={0}", decrementValue));
		}

		public void VolumeMuteToggle()
		{
			SetVolumeMute(!VolumeIsMuted);
		}

		public void SetVolumeMute(bool mute)
		{
			if (m_MuteControl == null)
			{
				Log(eSeverity.Error, "Unable to set mute state - Mute control is null");
				return;
			}

			m_MuteControl.SetValue(mute);
		}

		#endregion

		#region Private Methods

		private void Subscribe()
		{
			if (m_VolumeControl != null)
				m_VolumeControl.OnValueUpdated += VolumeControlOnValueUpdated;

			if (m_MuteControl != null)
				m_MuteControl.OnValueUpdated += MuteControlOnValueUpdated;
		}

		private void Unsubscribe()
		{
			if (m_VolumeControl != null)
				m_VolumeControl.OnValueUpdated -= VolumeControlOnValueUpdated;

			if (m_MuteControl != null)
				m_MuteControl.OnValueUpdated -= MuteControlOnValueUpdated;
		}

		private void VolumeControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			VolumeFeedback(args.ValueRaw, args.ValuePosition, args.ValueString);
		}

		private void MuteControlOnValueUpdated(object sender, ControlValueUpdateEventArgs args)
		{
			OnMuteStateChanged.Raise(this, new BoolEventArgs(BooleanNamedControl.GetValueAsBool(args.ValueRaw)));
		}

		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe();

			base.DisposeFinal(disposing);
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
