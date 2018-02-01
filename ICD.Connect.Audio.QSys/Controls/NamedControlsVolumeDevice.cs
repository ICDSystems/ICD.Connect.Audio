using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.QSys.CoreControl.NamedControl;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.Controls
{
    public sealed class NamedControlsVolumeDevice : AbstractVolumeLevelDeviceControl<QSysCoreDevice>, IVolumeMuteFeedbackDeviceControl
    {
	    private readonly string m_Name;
		
		private readonly NamedControl m_VolumeControl;

		private readonly BooleanNamedControl m_MuteControl;

		#region Properties

	    public override string Name { get { return m_Name; }  }
	    public override float VolumeRaw { get { return m_VolumeControl.ValueRaw; } }
	    public override float VolumePosition { get { return m_VolumeControl.ValuePosition; } }
	    public override string VolumeString { get { return m_VolumeControl.ValueString; } }
	    public bool VolumeIsMuted { get { return m_MuteControl.ValueBool; } }

	    #endregion

		#region Events

		public override event EventHandler<VolumeDeviceVolumeChangedEventArgs> OnVolumeChanged;
	    public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		public NamedControlsVolumeDevice(QSysCoreDevice qSysCore, string name, int id, NamedControl volumeControl, BooleanNamedControl muteControl) : base(qSysCore, id)
		{
			m_Name = name;
		    m_VolumeControl = volumeControl;
		    m_MuteControl = muteControl;
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
		    OnVolumeChanged.Raise(this, new VolumeDeviceVolumeChangedEventArgs(args.ValueRaw, args.ValuePosition, args.ValueString));
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
	}
}
