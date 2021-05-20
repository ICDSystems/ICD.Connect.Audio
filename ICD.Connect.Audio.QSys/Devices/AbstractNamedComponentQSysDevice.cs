using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.QSys.Devices.QSysCore;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.QSys.Devices
{
	public abstract class AbstractNamedComponentQSysDevice<TSettings, TNamedComponent> : AbstractDevice<TSettings>, INamedComponentQSysDevice<TNamedComponent> where TNamedComponent: class, INamedComponent where TSettings: INamedComponentQSysDeviceSettings, new()
	{
		[CanBeNull]
		private QSysCoreDevice m_Dsp;

		/// <summary>
		/// Primary component in the DSP the device is referencing
		/// </summary>
		private TNamedComponent m_NamedComponent;

		/// <summary>
		/// Raised when the named component changes
		/// </summary>
		public event EventHandler OnNamedComponentChanged;

		/// <summary>
		/// Raised when the parent DSP changes.
		/// </summary>
		public event EventHandler OnDspChanged;

		[CanBeNull]
		public TNamedComponent NamedComponent
		{
			get { return m_NamedComponent;}
			private set
			{
				if (m_NamedComponent == value)
					return;

				Unsubscribe(m_NamedComponent);
				m_NamedComponent = value;
				Subscribe(m_NamedComponent);

				OnNamedComponentChanged.Raise(this);
			}
		}

		[CanBeNull]
		protected string NamedComponentName { get; private set; }

		/// <summary>
		/// Primary component in the DSP the device is referencing
		/// </summary>
		INamedComponent INamedComponentQSysDevice.NamedComponent { get { return NamedComponent; } }

		[CanBeNull]
		public QSysCoreDevice Dsp
		{
			get { return m_Dsp; }
			private set
			{
				if (m_Dsp == value)
					return;

				Unsubscribe(m_Dsp);
				m_Dsp = value;
				Subscribe(m_Dsp);
				
				UpdateDspControls();

				UpdateCachedOnlineStatus();

				OnDspChanged.Raise(this);
			}
		}

		protected virtual void UpdateDspControls()
		{
			NamedComponent =
				Dsp == null || NamedComponentName == null
					? null
					: Dsp.Components.LazyLoadNamedComponent<TNamedComponent>(NamedComponentName);
		}

		protected virtual void Subscribe(TNamedComponent component)
		{
		}

		protected virtual void Unsubscribe(TNamedComponent component)
		{
		}

		/// <summary>
		/// Subscribe to the dsp events.
		/// </summary>
		/// <param name="dsp"></param>
		private void Subscribe(QSysCoreDevice dsp)
		{
			if (dsp == null)
				return;

			dsp.OnIsOnlineStateChanged += DspOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the dsp events.
		/// </summary>
		/// <param name="dsp"></param>
		private void Unsubscribe(QSysCoreDevice dsp)
		{
			if (dsp == null)
				return;

			dsp.OnIsOnlineStateChanged -= DspOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the dsp changes online status.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void DspOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return Dsp != null && Dsp.IsOnline && NamedComponent != null;
		}

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			NamedComponent = null;
			NamedComponentName = null;
			Dsp = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			NamedComponentName = settings.ComponentName;

			try
			{
				Dsp = factory.GetOriginatorById<QSysCoreDevice>(settings.DspId);
			}
			catch (KeyNotFoundException)
			{
				Logger.Log(eSeverity.Error, "No QSys Core Device with id {0}", settings.DspId);
			}
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.DspId = m_Dsp == null ? 0 : m_Dsp.Id;
			settings.ComponentName = NamedComponentName;
		}

		#endregion
	}
}
