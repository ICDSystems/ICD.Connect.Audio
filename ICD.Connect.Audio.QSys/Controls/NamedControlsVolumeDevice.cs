using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.Controls
{
    class NamedControlsVolumeDevice : IVolumeDeviceControl
    {
	    public IDeviceBase Parent { get; }
	    public int Id { get; }
	    public string Name { get; }
	    public DeviceControlInfo DeviceControlInfo { get; }
	    public event EventHandler<FloatEventArgs> OnRawVolumeChanged;
	    public event EventHandler<BoolEventArgs> OnMuteStateChanged;
	    public float RawVolume { get; }
	    public float RawVolumeMin { get; }
	    public float RawVolumeMax { get; }
	    public float? RawVolumeSafetyMin { get; set; }
	    public float? RawVolumeSafetyMax { get; set; }
	    public float? RawVolumeDefault { get; set; }
	    public bool IsMuted { get; }
	    public void SetRawVolume(float volume)
	    {
		    throw new NotImplementedException();
	    }

	    public void SetMute(bool mute)
	    {
		    throw new NotImplementedException();
	    }

	    public void RawVolumeIncrement()
	    {
		    throw new NotImplementedException();
	    }

	    public void RawVolumeDecrement()
	    {
		    throw new NotImplementedException();
	    }

	    #region Console

	    public string ConsoleName { get; }
	    public string ConsoleHelp { get; }
	    public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
	    {
		    throw new NotImplementedException();
	    }

	    public void BuildConsoleStatus(AddStatusRowDelegate addRow)
	    {
		    throw new NotImplementedException();
	    }

	    public IEnumerable<IConsoleCommand> GetConsoleCommands()
	    {
		    throw new NotImplementedException();
	    }

	    #endregion

	    public void Dispose()
	    {
		    throw new NotImplementedException();
	    }

	    public bool IsDisposed { get; }
    }
}
