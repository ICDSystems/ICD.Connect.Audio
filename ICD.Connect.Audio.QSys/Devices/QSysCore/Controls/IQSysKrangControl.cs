﻿using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.Controls
{
	public interface IQSysKrangControl : IDeviceControl
	{
		// Also needs to implement constructor like:
		// IQSysKrangControl(int id, string friendlyName, CoreElementsLoadContext context, string xml);
	}
}