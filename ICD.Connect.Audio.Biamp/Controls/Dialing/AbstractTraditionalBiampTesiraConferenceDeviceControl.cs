using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing
{
	public abstract class AbstractBiampTesiraConferenceDeviceControl : AbstractTraditionalConferenceDeviceControl<BiampTesiraDevice>,
	                                                                IBiampTesiraConferenceDeviceControl
	{
		private readonly string m_Name;
		
		[CanBeNull]
		private readonly IBiampTesiraStateDeviceControl m_PrivacyMuteControl;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="parent"></param>
		/// <param name="privacyMuteControl"></param>
		protected AbstractBiampTesiraConferenceDeviceControl(int id, Guid uuid, string name, BiampTesiraDevice parent,
		                                                     IBiampTesiraStateDeviceControl privacyMuteControl)
			: base(parent, id, uuid)
		{
			m_Name = name;

			m_PrivacyMuteControl = privacyMuteControl;
			if (m_PrivacyMuteControl != null)
				SupportedConferenceFeatures |= eConferenceFeatures.PrivacyMute;

			SubscribePrivacyMute(m_PrivacyMuteControl);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			UnsubscribePrivacyMute(m_PrivacyMuteControl);
		}

		#region Methods

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			if (m_PrivacyMuteControl == null)
			{
				Logger.Log(eSeverity.Error, "{0} unable to set Privacy Mute - control is null", Name);
				return;
			}

			m_PrivacyMuteControl.SetState(enabled);
		}

		#endregion

		#region Privacy Mute Callbacks

		private void SubscribePrivacyMute(IBiampTesiraStateDeviceControl privacyMuteControl)
		{
			if (privacyMuteControl == null)
				return;

			privacyMuteControl.OnStateChanged += PrivacyMuteControlOnStateChanged;
		}

		private void UnsubscribePrivacyMute(IBiampTesiraStateDeviceControl privacyMuteControl)
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

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("PrivacyMute Control", m_PrivacyMuteControl);
		}

		#endregion
	}
}
