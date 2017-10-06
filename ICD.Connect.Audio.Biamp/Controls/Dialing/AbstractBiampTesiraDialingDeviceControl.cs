using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Services.Logging;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing
{
	public abstract class AbstractBiampTesiraDialingDeviceControl : AbstractDialingDeviceControl<BiampTesiraDevice>,
	                                                                IBiampTesiraDialingDeviceControl
	{
		private readonly string m_Name;
		private readonly BiampTesiraStateDeviceControl m_DoNotDisturbControl;
		private readonly BiampTesiraStateDeviceControl m_PrivacyMuteControl;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="parent"></param>
		/// <param name="doNotDisturbControl"></param>
		/// <param name="privacyMuteControl"></param>
		protected AbstractBiampTesiraDialingDeviceControl(int id, string name, BiampTesiraDevice parent,
		                                                  BiampTesiraStateDeviceControl doNotDisturbControl,
		                                                  BiampTesiraStateDeviceControl privacyMuteControl)
			: base(parent, id)
		{
			m_Name = name;

			m_DoNotDisturbControl = doNotDisturbControl;
			m_PrivacyMuteControl = privacyMuteControl;

			SubscribeDoNotDisturb(m_DoNotDisturbControl);
			SubscribePrivacyMute(m_PrivacyMuteControl);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			UnsubscribeDoNotDisturb(m_DoNotDisturbControl);
			UnsubscribePrivacyMute(m_PrivacyMuteControl);
		}

		#region Methods

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public override void Dial(string number, eConferenceSourceType callType)
		{
			switch (callType)
			{
				case eConferenceSourceType.Audio:
					Dial(number);
					break;

				default:
					throw new ArgumentOutOfRangeException("callType");
			}
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			if (m_DoNotDisturbControl == null)
			{
				Parent.Log(eSeverity.Error, "{0} unable to set Do-Not-Disturb - control is null", Name);
				return;
			}

			m_DoNotDisturbControl.SetState(enabled);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			if (m_PrivacyMuteControl == null)
			{
				Parent.Log(eSeverity.Error, "{0} unable to set Privacy Mute - control is null", Name);
				return;
			}

			m_PrivacyMuteControl.SetState(enabled);
		}

		#endregion

		#region Do Not Disturb Callbacks

		private void SubscribeDoNotDisturb(BiampTesiraStateDeviceControl doNotDisturbControl)
		{
			if (doNotDisturbControl == null)
				return;

			doNotDisturbControl.OnStateChanged += DoNotDisturbControlOnStateChanged;
		}

		private void UnsubscribeDoNotDisturb(BiampTesiraStateDeviceControl doNotDisturbControl)
		{
			if (doNotDisturbControl == null)
				return;

			doNotDisturbControl.OnStateChanged -= DoNotDisturbControlOnStateChanged;
		}

		private void DoNotDisturbControlOnStateChanged(object sender, BoolEventArgs args)
		{
			DoNotDisturb = args.Data;
		}

		#endregion

		#region Privacy Mute Callbacks

		private void SubscribePrivacyMute(BiampTesiraStateDeviceControl privacyMuteControl)
		{
			if (privacyMuteControl == null)
				return;

			privacyMuteControl.OnStateChanged += PrivacyMuteControlOnStateChanged;
		}

		private void UnsubscribePrivacyMute(BiampTesiraStateDeviceControl privacyMuteControl)
		{
			if (privacyMuteControl == null)
				return;

			privacyMuteControl.OnStateChanged -= PrivacyMuteControlOnStateChanged;
		}

		private void PrivacyMuteControlOnStateChanged(object sender, BoolEventArgs args)
		{
			PrivacyMuted = args.Data;
		}

		#endregion
	}
}
