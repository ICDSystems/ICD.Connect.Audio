using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Audio.Biamp.Tesira.Devices
{
	public abstract class AbstractTesiraChildDevice<TSettings> : AbstractDevice<TSettings>
		where TSettings : ITesiraChildDeviceSettings, new()
	{
		/// <summary>
		/// Raised when the parent Biamp device changes.
		/// </summary>
		public event EventHandler OnBiampChanged;

		[CanBeNull]
		private BiampTesiraDevice m_Biamp;

		[CanBeNull]
		public BiampTesiraDevice Biamp {get { return m_Biamp; }}

		protected override void DisposeFinal(bool disposing)
		{
			OnBiampChanged = null;

			base.DisposeFinal(disposing);
		}

		protected override bool GetIsOnlineStatus()
		{
			return m_Biamp != null && m_Biamp.IsOnline;
		}

		protected override void ClearSettingsFinal()
		{
			SetBiamp(null);

			base.ClearSettingsFinal();
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.BiampId = m_Biamp == null ? null : (int?)m_Biamp.Id;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			BiampTesiraDevice biamp = null;

			if (settings.BiampId.HasValue)
			{
				try
				{
					biamp = factory.GetOriginatorById<BiampTesiraDevice>(settings.BiampId.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Biamp Tesira Device with id {0}", settings.BiampId.Value);
				}
			}

			SetBiamp(biamp);
		}

		[PublicAPI]
		public void SetBiamp(BiampTesiraDevice biamp)
		{
			Unsubscribe(m_Biamp);
			m_Biamp = biamp;
			Subscribe(m_Biamp);

			UpdateCachedOnlineStatus();
			OnBiampChanged.Raise(this);
		}

		#region Biamp Callbacks

		private void Subscribe(BiampTesiraDevice biamp)
		{
			if (biamp == null)
				return;

			biamp.OnIsOnlineStateChanged += BiampOnOnIsOnlineStateChanged;
		}

		private void Unsubscribe(BiampTesiraDevice biamp)
		{
			if (biamp == null)
				return;

			biamp.OnIsOnlineStateChanged -= BiampOnOnIsOnlineStateChanged;
		}

		private void BiampOnOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion
	}
}