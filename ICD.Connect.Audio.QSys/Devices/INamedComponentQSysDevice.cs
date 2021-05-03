using System;
using ICD.Common.Properties;
using ICD.Connect.Audio.QSys.Devices.QSysCore;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Devices;

namespace ICD.Connect.Audio.QSys.Devices
{
	public interface INamedComponentQSysDevice<TNamedComponent> : INamedComponentQSysDevice where TNamedComponent : class, INamedComponent
	{
		/// <summary>
		/// Primary component in the DSP the device is referencing
		/// </summary>
		[CanBeNull]
		new TNamedComponent NamedComponent { get; }

	}

	public interface INamedComponentQSysDevice : IDevice
	{
		/// <summary>
		/// Raised when the named component changes
		/// </summary>
		event EventHandler OnNamedComponentChanged;

		/// <summary>
		/// Primary component in the DSP the device is referencing
		/// </summary>
		[CanBeNull]
		INamedComponent NamedComponent { get; }

		/// <summary>
		/// QSys DSP parent
		/// </summary>
		[CanBeNull]
		QSysCoreDevice Dsp { get; }

		/// <summary>
		/// Raised when the parent DSP changes.
		/// </summary>
		event EventHandler OnDspChanged;
	}
}