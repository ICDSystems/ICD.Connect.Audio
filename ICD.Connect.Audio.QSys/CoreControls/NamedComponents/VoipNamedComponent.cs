using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
	/// <summary>
	/// Controls a QSys VoIP block via Named Component
	/// </summary>
	public sealed class VoipNamedComponent : AbstractNamedComponent
	{

		public const string CONTROL_CALL_NUMBER = "call.number";
		public const string CONTROL_CALL_STATUS = "call.status";
		public const string CONTROL_CALL_AUTOANSWER = "call.autoanswer";
		public const string CONTROL_CALL_CID_NAME = "call.cid.name";
		public const string CONTROL_CALL_CID_NUMBER = "call.cid.number";
		public const string CONTROL_CALL_DETAILS = "call.details";
		public const string CONTROL_CALL_CONNECT_TIME = "call.connect.time";
		public const string CONTROL_CALL_STATE = "call.state";
		public const string CONTROL_CALL_OFFHOOK = "call.offhook";
		public const string CONTROL_CALL_RING = "call.ring";
		public const string CONTROL_CALL_RINGING = "call.ringing";
		public const string CONTROL_CALL_PAD_0 = "call.pinpad.0";
		public const string CONTROL_CALL_PAD_1 = "call.pinpad.1";
		public const string CONTROL_CALL_PAD_2 = "call.pinpad.2";
		public const string CONTROL_CALL_PAD_3 = "call.pinpad.3";
		public const string CONTROL_CALL_PAD_4 = "call.pinpad.4";
		public const string CONTROL_CALL_PAD_5 = "call.pinpad.5";
		public const string CONTROL_CALL_PAD_6 = "call.pinpad.6";
		public const string CONTROL_CALL_PAD_7 = "call.pinpad.7";
		public const string CONTROL_CALL_PAD_8 = "call.pinpad.8";
		public const string CONTROL_CALL_PAD_9 = "call.pinpad.9";
		public const string CONTROL_CALL_PAD_STAR = "call.pinpad.*";
		public const string CONTROL_CALL_PAD_POUND = "call.pinpad.#";
		public const string CONTROL_CALL_CONNECT = "call.connect";
		public const string CONTROL_CALL_DISCONNECT = "call.disconnect";
		public const string CONTROL_CALL_DND = "call.dnd";

		public event EventHandler<ControlValueUpdateEventArgs> OnControlValueUpdated ;

		#region Constructors
		/// <summary>
		/// Constructor for Explicitly defined component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public VoipNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, friendlyName, id)
		{
			string componentName = XmlUtils.TryReadChildElementContentAsString(xml, "ComponentName");

			// If we don't have a component name, bail out
			if (String.IsNullOrEmpty(componentName))
				throw new InvalidOperationException(String.Format("Tried to create VoipNamedComponent {0}:{1} without component name", id, friendlyName));

			ComponentName = componentName;
			AddVoipControls();
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		/// <summary>
		/// Constructor for Implicitly built component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="context"></param>
		/// <param name="componentName"></param>
		[UsedImplicitly]
		public VoipNamedComponent(int id, CoreElementsLoadContext context, string componentName)
			: base(context.QSysCore, String.Format("Implicit:{0}", componentName), id)

		{
			ComponentName = componentName;
			AddVoipControls();
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());

		}

		#endregion

		#region Controls

		private void AddVoipControls()
		{
			AddControl(new NamedComponentControl(this, CONTROL_CALL_NUMBER));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_STATUS));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_AUTOANSWER));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_CID_NAME));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_CID_NUMBER));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_DETAILS));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_CONNECT_TIME));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_STATE));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_OFFHOOK));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_RING));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_RINGING));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_0));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_1));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_2));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_3));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_4));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_5));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_6));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_7));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_8));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_9));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_STAR));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_PAD_POUND));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_CONNECT));
			AddControl(new NamedComponentControl(this, CONTROL_CALL_DISCONNECT));



			SubscribeControls();
		}

		private void SubscribeControls()
		{
			foreach (INamedComponentControl control in GetControlsForSubscribe())
			{
				control.OnValueUpdated += ControlOnValueUpdated;
			}
		}

		private void UnsubscribeControls()
		{
			foreach (INamedComponentControl control in GetControlsForSubscribe())
			{
				control.OnValueUpdated -= ControlOnValueUpdated;
			}
		}

		private void ControlOnValueUpdated(object sender, ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			OnControlValueUpdated.Raise(sender, controlValueUpdateEventArgs);
		}

#endregion

		#region console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return String.Format("VoIPComponent:{0}", Name); } }

		#endregion


	}
}