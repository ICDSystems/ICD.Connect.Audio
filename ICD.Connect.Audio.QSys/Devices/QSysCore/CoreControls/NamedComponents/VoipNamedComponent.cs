using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
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

		private static readonly IcdHashSet<string> s_Controls =
			new IcdHashSet<string>
			{
				CONTROL_CALL_NUMBER,
				CONTROL_CALL_STATUS,
				CONTROL_CALL_AUTOANSWER,
				CONTROL_CALL_CID_NAME,
				CONTROL_CALL_CID_NUMBER,
				CONTROL_CALL_DETAILS,
				//CONTROL_CALL_CONNECT_TIME,
				CONTROL_CALL_STATE,
				CONTROL_CALL_OFFHOOK,
				CONTROL_CALL_RING,
				CONTROL_CALL_RINGING,
				CONTROL_CALL_PAD_0,
				CONTROL_CALL_PAD_1,
				CONTROL_CALL_PAD_2,
				CONTROL_CALL_PAD_3,
				CONTROL_CALL_PAD_4,
				CONTROL_CALL_PAD_5,
				CONTROL_CALL_PAD_6,
				CONTROL_CALL_PAD_7,
				CONTROL_CALL_PAD_8,
				CONTROL_CALL_PAD_9,
				CONTROL_CALL_PAD_STAR,
				CONTROL_CALL_PAD_POUND,
				CONTROL_CALL_CONNECT,
				CONTROL_CALL_DISCONNECT,
				CONTROL_CALL_DND
			};

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
			if (string.IsNullOrEmpty(componentName))
				throw new InvalidOperationException(string.Format("Tried to create VoipNamedComponent {0}:{1} without component name", id, friendlyName));

			ComponentName = componentName;
			AddControls(s_Controls);
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
			: base(context.QSysCore, string.Format("Implicit:{0}", componentName), id)

		{
			ComponentName = componentName;
			AddControls(s_Controls);
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return string.Format("VoIPComponent:{0}", Name); } }

		#endregion
	}
}