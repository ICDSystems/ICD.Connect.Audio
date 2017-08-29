using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.IoBlocks.TelephoneInterface;
using ICD.Connect.Audio.Biamp.Controls.State;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Audio.Biamp.Controls.Dialing.Telephone
{
	public sealed class TiDialingDeviceControl : AbstractBiampTesiraDialingDeviceControl
	{
		/// <summary>
		/// Raised when the hold state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnHoldChanged;

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		private readonly TiControlStatusBlock m_TiControl;
		private readonly BiampTesiraStateDeviceControl m_HoldControl;

		private bool m_Hold;

		private TesiraConferenceSource m_ActiveSource;
		private readonly SafeCriticalSection m_ActiveSourceSection;

		#region Properties

		/// <summary>
		/// Gets the hold state.
		/// </summary>
		[PublicAPI]
		public bool IsOnHold
		{
			get { return m_Hold; }
			protected set
			{
				if (value == m_Hold)
					return;

				m_Hold = value;

				OnHoldChanged.Raise(this, new BoolEventArgs(m_Hold));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="tiControl"></param>
		/// <param name="doNotDisturbControl"></param>
		/// <param name="privacyMuteControl"></param>
		/// <param name="holdControl"></param>
		public TiDialingDeviceControl(int id, string name, TiControlStatusBlock tiControl,
		                              BiampTesiraStateDeviceControl doNotDisturbControl,
		                              BiampTesiraStateDeviceControl privacyMuteControl,
		                              BiampTesiraStateDeviceControl holdControl)
			: base(id, name, tiControl.Device, doNotDisturbControl, privacyMuteControl)
		{
			m_ActiveSourceSection = new SafeCriticalSection();

			m_TiControl = tiControl;
			m_HoldControl = holdControl;

			Subscribe(m_TiControl);
			Subscribe(m_HoldControl);
		}

		#region Methods

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Audio; } }

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			m_ActiveSourceSection.Enter();

			try
			{
				return m_ActiveSource == null
					       ? Enumerable.Empty<IConferenceSource>()
					       : new IConferenceSource[] {m_ActiveSource};
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			m_TiControl.Dial(number);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			m_TiControl.SetAutoAnswer(enabled);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceAdded = null;
			OnHoldChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_TiControl);
			Unsubscribe(m_HoldControl);

			ClearCurrentSource();
		}

		/// <summary>
		/// Sets the hold state.
		/// </summary>
		/// <param name="hold"></param>
		private void SetHold(bool hold)
		{
			if (m_HoldControl == null)
			{
				Parent.Log(eSeverity.Error, "{0} unable to hold - control is null", Name);
				return;
			}

			m_HoldControl.SetState(hold);
		}

		#endregion

		#region Sources

		/// <summary>
		/// Instantiates a new active source.
		/// </summary>
		private void CreateActiveSource()
		{
			IConferenceSource source;

			m_ActiveSourceSection.Enter();

			try
			{
				ClearCurrentSource();

				m_ActiveSource = new TesiraConferenceSource
				{
					AnswerCallback = AnswerCallback,
					HoldCallback = HoldCallback,
					ResumeCallback = ResumeCallback,
					HangupCallback = HangupCallback,
					SendDtmfCallback = SendDtmfCallback
				};

				// Setup the source properties
				throw new NotImplementedException();

				source = m_ActiveSource;
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}

			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
		}

		/// <summary>
		/// Unsubscribes from the current source and clears the field.
		/// </summary>
		private void ClearCurrentSource()
		{
			m_ActiveSourceSection.Enter();

			try
			{
				if (m_ActiveSource == null)
					return;

				m_ActiveSource.AnswerCallback = null;
				m_ActiveSource.HoldCallback = null;
				m_ActiveSource.ResumeCallback = null;
				m_ActiveSource.HangupCallback = null;
				m_ActiveSource.SendDtmfCallback = null;
			}
			finally
			{
				m_ActiveSourceSection.Leave();
			}
		}

		private void SendDtmfCallback(string keys)
		{
			keys.ForEach(c => m_TiControl.Dtmf(c));
		}

		private void HangupCallback()
		{
			m_TiControl.End();
		}

		private void ResumeCallback()
		{
			SetHold(false);
		}

		private void HoldCallback()
		{
			SetHold(true);
		}

		private void AnswerCallback()
		{
			m_TiControl.Answer();
		}

		#endregion

		#region Hold Control Callbacks

		/// <summary>
		/// Subscribe to the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void Subscribe(BiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			m_HoldControl.OnStateChanged += HoldControlOnStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the hold control events.
		/// </summary>
		/// <param name="holdControl"></param>
		private void Unsubscribe(BiampTesiraStateDeviceControl holdControl)
		{
			if (holdControl == null)
				return;

			m_HoldControl.OnStateChanged -= HoldControlOnStateChanged;
		}

		/// <summary>
		/// Called when the hold control state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void HoldControlOnStateChanged(object sender, BoolEventArgs args)
		{
			IsOnHold = args.Data;
		}

		#endregion

		#region Attribute Interface Callbacks

		/// <summary>
		/// Subscribe to the dialer block events.
		/// </summary>
		/// <param name="attributeInterface"></param>
		private void Subscribe(TiControlStatusBlock attributeInterface)
		{
			attributeInterface.OnAutoAnswerChanged += AttributeInterfaceOnAutoAnswerChanged;
		}

		/// <summary>
		/// Unsubscribe to the dialer block events.
		/// </summary>
		/// <param name="attributeInterface"></param>
		private void Unsubscribe(TiControlStatusBlock attributeInterface)
		{
			attributeInterface.OnAutoAnswerChanged -= AttributeInterfaceOnAutoAnswerChanged;
		}

		private void AttributeInterfaceOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			AutoAnswer = args.Data;
		}

		#endregion
	}
}
