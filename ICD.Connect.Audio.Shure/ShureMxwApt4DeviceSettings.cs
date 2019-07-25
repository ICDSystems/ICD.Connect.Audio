using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Audio.Shure
{
	[KrangSettings("ShureMxwApt4", typeof(ShureMxwApt4Device))]
	public class ShureMxwApt4DeviceSettings : AbstractShureMicDeviceSettings
	{
	}
}