using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControls;
using ICD.Connect.Audio.Controls;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.Controls
{
    public sealed class NamedControlsVolumeDevice : AbstractVolumeLevelDeviceControl<QSysCoreDevice>, IVolumeMuteFeedbackDeviceControl, IQSysKrangControl
    {
	    private readonly string m_Name;
		
		private readonly INamedControl m_VolumeControl;

		private readonly BooleanNamedControl m_MuteControl;

		#region Properties

	    public override string Name { get { return m_Name; }  }
	    public override float VolumeRaw { get { return m_VolumeControl.ValueRaw; } }
	    public override float VolumePosition { get { return m_VolumeControl.ValuePosition; } }
	    public override string VolumeString { get { return m_VolumeControl.ValueString; } }
	    public bool VolumeIsMuted { get { return m_MuteControl.ValueBool; } }

	    #endregion

		#region Events

	    public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		public NamedControlsVolumeDevice(QSysCoreDevice qSysCore, string name, int id, INamedControl volumeControl, BooleanNamedControl muteControl) : base(qSysCore, id)
		{
			m_Name = name;
		    m_VolumeControl = volumeControl;
		    m_MuteControl = muteControl;
			Subscribe();
	    }

		/// <summary>
		/// Constructor used to load control from xml
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
	    public NamedControlsVolumeDevice(int id, string friendlyName, CoreElementsLoadContext context, string xml)
		    : base(context.QSysCore, id)
		{
			m_Name = friendlyName;

			string volumeName = XmlUtils.TryReadChildElementContentAsString(xml, "VolumeControlName");
			string muteName = XmlUtils.TryReadChildElementContentAsString(xml, "MuteControlName");
			float? incrementValue = XmlUtils.TryReadChildElementContentAsFloat(xml, "IncrementValue");
			int? repeatBeforeTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBeforeTime");
			int? repeatBetweenTime = XmlUtils.TryReadChildElementContentAsInt(xml, "RepeatBetweenTime");

			// If we don't have a volume or mute control names, bail out
			if (String.IsNullOrEmpty(volumeName) | String.IsNullOrEmpty(muteName))
				throw new InvalidOperationException(String.Format("Tried to create NamedControlVolumeDevice {0}:{1} without volume or mute",id, friendlyName));

			// Load volume/mute controls
			m_VolumeControl = context.LazyLoadNamedControl(volumeName, typeof(NamedControl)) as NamedControl;
			m_MuteControl = context.LazyLoadNamedControl(muteName, typeof(BooleanNamedControl)) as BooleanNamedControl;
			
			if (m_VolumeControl == null)
				throw new KeyNotFoundException(String.Format("QSys - No Volume Control {0}", volumeName));
			if (m_MuteControl == null)
				throw new KeyNotFoundException(String.Format("QSys - No Mute Control {0}", muteName));


			if (incrementValue != null)
				IncrementValue = (float)incrementValue;
			if (repeatBeforeTime != null)
				RepeatBeforeTime = (int)repeatBeforeTime;
			if (repeatBetweenTime != null)
				RepeatBetweenTime = (int)repeatBetweenTime;

			Subscribe();
	    }

	    #region Methods

	    public override void SetVolumeRaw(float volume)
	    {
		    m_VolumeControl.SetValue(volume);
	    }

	    public override void SetVolumePosition(float position)
	    {
		    m_VolumeControl.SetPosition(position);
	    }

	    public override void VolumeLevelIncrement(float incrementValue)
	    {
		    m_VolumeControl.SetValue(string.Format("+={0}", incrementValue));
	    }

	    public override void VolumeLevelDecrement(float decrementValue)
	    {
		    m_VolumeControl.SetValue(string.Format("-={0}", decrementValue));
	    }

	    public void VolumeMuteToggle()
	    {
		    SetVolumeMute(!VolumeIsMuted);
	    }

	    public void SetVolumeMute(bool mute)
	    {
		    m_MuteControl.SetValue(mute);
	    }

	    #endregion

		#region Private Methods

	    private void Subscribe()
	    {
		    m_VolumeControl.OnValueUpdated += VolumeControlOnValueUpdated;
			m_MuteControl.OnValueUpdated += MuteControlOnValueUpdated;
	    }

	    private void Unsubscribe()
	    {
		    m_VolumeControl.OnValueUpdated -= VolumeControlOnValueUpdated;
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
