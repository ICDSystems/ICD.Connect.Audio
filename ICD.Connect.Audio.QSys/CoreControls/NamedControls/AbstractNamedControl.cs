using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Rpc;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedControls
{
    /// <summary>
    /// Represents a NamedControl
    /// </summary>
    public abstract class AbstractNamedControl : AbstractCoreControl, INamedControl
	{
	    /// <summary>
        /// Name of the control on the QSys Core
        /// </summary>
        public string ControlName { get; private set; }

        /// <summary>
        /// String representation of the control value
        /// </summary>
        public string ValueString { get; private set; }

        /// <summary>
        /// Float representation of the control value
        /// </summary>
        public float ValueRaw { get; private set; }

        /// <summary>
        /// Position representation of the control value
        /// This is a number between 0 and 1
        /// Representing the relative position of the control
        /// </summary>
        public float ValuePosition { get; private set; }

		#region events

	    public event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		#endregion

		#region methods

	    public void TriggerControl()
	    {
		    SetPosition(1);
	    }

	    /// <summary>
		/// Sets the value of the control
		/// ToString() method is used to send the value
		/// </summary>
		/// <param name="value">Value to set the control to</param>
		public void SetValue(object value)
        {
            SendData(new ControlSetValueRpc(this, value).Serialize());
        }

	    public void SetPosition(float position)
	    {
			SendData(new ControlSetPositionRpc(this, position).Serialize());
		}

	    /// <summary>
		/// Polls the value of the control from the QSys Core
		/// </summary>
		public void PollValue()
        {
            SendData(new ControlGetRpc(this).Serialize());
        }

	    public void ParseFeedback(JToken feedback)
	    {
		    if (feedback == null)
			    throw new ArgumentNullException("feedback");

		    string valueString = (string)feedback.SelectToken("String");
		    float valueValue = (float)feedback.SelectToken("Value");
		    float valuePostion = (float)feedback.SelectToken("Position");

		    SetFeedback(valueString, valueValue, valuePostion);
		}

		#endregion

		#region internal methods

		/// <summary>
		/// Called by the Core to update feedback for the control
		/// </summary>
		/// <param name="valueString">String value of the control</param>
		/// <param name="valueRaw">Raw value of the control</param>
		/// <param name="valuePosition">Position value of the control</param>
		internal void SetFeedback(string valueString, float valueRaw, float valuePosition)
        {
            ValueString = valueString;
            ValueRaw = valueRaw;
            ValuePosition = valuePosition;

	        OnValueUpdated.Raise(this, new ControlValueUpdateEventArgs(ControlName, ValueString, ValueRaw, ValuePosition));
		}

		/// <summary>
		/// Subscribes this control to the initial change groups.
		/// </summary>
		/// <param name="loadContext">Load context, to lookup change groups and global default change groups</param>
		/// <param name="controlChangeGroups">Additional change groups needed for this control</param>
		private void SetupInitialChangeGroups(CoreElementsLoadContext loadContext,
														IEnumerable<int> controlChangeGroups)
		{
			// Get Default Change Groups from Load Context
			// We want to subscribe to all of them
			IcdHashSet<int> changeGroups = new IcdHashSet<int>(loadContext.GetDefaultChangeGroups());

			// If this control has a specific change group,
			// Add it to the hash and subscribe to it
			changeGroups.AddRange(controlChangeGroups);

			// Do some subscribing
			foreach (int changeGroupId in changeGroups)
			{
				IChangeGroup changeGroup = loadContext.TryGetChangeGroup(changeGroupId);
				if (changeGroup != null)
					changeGroup.AddNamedControl(this);
				else
					QSysCore.Log(eSeverity.Warning, "NamedControl {0} couldn't add to change group id {1} - not found", Id, changeGroupId);
			}
		}

	    #endregion

		/// <summary>
		/// Constructor for Explicitly Defined Elements
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="loadContext"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		protected AbstractNamedControl(int id, string name, CoreElementsLoadContext loadContext, string xml)
			: base(loadContext.QSysCore, name, id)
		{
			if (loadContext == null)
				throw new ArgumentNullException("loadContext");

			string controlName = XmlUtils.GetAttributeAsString(xml, "controlName");

			ControlName = controlName;

			int? changeGroupId = null;
			try
			{
				changeGroupId = XmlUtils.GetAttributeAsInt(xml, "changeGroup");
			}
			catch (FormatException e)
			{
			}

			IEnumerable<int> controlChangeGroups;
			if (changeGroupId == null)
				controlChangeGroups = Enumerable.Empty<int>();
			else
				controlChangeGroups = ((int)changeGroupId).Yield();

			SetupInitialChangeGroups(loadContext, controlChangeGroups);

			Subscribe(loadContext.QSysCore);

			if (loadContext.QSysCore.Initialized)
				PollValue();
		}

		/// <summary>
		/// Constructor for Implicitly Defined Elements
		/// </summary>
		/// <param name="id"></param>
		/// <param name="loadContext"></param>
		/// <param name="controlName"></param>
		[UsedImplicitly]
		protected AbstractNamedControl(int id, CoreElementsLoadContext loadContext, string controlName)
		    : base(loadContext.QSysCore, String.Format("Implicit:{0}", controlName), id)
		{
			ControlName = controlName;
			SetupInitialChangeGroups(loadContext, Enumerable.Empty<int>());

			Subscribe(loadContext.QSysCore);

			if (loadContext.QSysCore.Initialized)
				PollValue();
		}

	    /// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
	    public override void DisposeFinal(bool disposing)
	    {
		    OnValueUpdated = null;

		    base.DisposeFinal(disposing);

		    Unsubscribe(QSysCore);
	    }

	    #region QSys Core Callbacks

		/// <summary>
		/// Subscribe to the QSys Core events.
		/// </summary>
		/// <param name="qSysCore"></param>
	    private void Subscribe(QSysCoreDevice qSysCore)
	    {
		    qSysCore.OnInitializedChanged += QSysCoreOnInitializedChanged;
	    }

		/// <summary>
		/// Unsubscribe from the QSys Core events.
		/// </summary>
		/// <param name="qSysCore"></param>
	    private void Unsubscribe(QSysCoreDevice qSysCore)
	    {
		    qSysCore.OnInitializedChanged -= QSysCoreOnInitializedChanged;
	    }

		/// <summary>
		/// Called when the QSys Core initialization state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
	    private void QSysCoreOnInitializedChanged(object sender, BoolEventArgs boolEventArgs)
	    {
		    if (QSysCore.Initialized)
			    PollValue();
	    }

	    #endregion

	    #region Console

	    public override string ConsoleHelp { get { return string.Format("NamedControl: {0}" ,ControlName); } }

	    public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
	    {
			base.BuildConsoleStatus(addRow);
		    addRow("Control Name", ControlName);
		    addRow("Value String", ValueString);
		    addRow("Value Raw", ValueRaw);
		    addRow("Value Position", ValuePosition);
	    }

	    public override IEnumerable<IConsoleCommand> GetConsoleCommands()
	    {
		    foreach (IConsoleCommand command in GetBaseConsoleCommands())
			    yield return command;
		    yield return new ConsoleCommand("Poll", "Pull The Current Value", () => PollValue());
		    yield return new ConsoleCommand("Trigger", "Triggers the control", () => TriggerControl());
			yield return new GenericConsoleCommand<string>("SetPosition", "SetPosition <Position>" ,p => SetPosition(float.Parse(p)));
			yield return new GenericConsoleCommand<string>("SetValue", "SetValue <Value>", p => SetValue(p));
	    }

	    private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
	    {
		    return base.GetConsoleCommands();
	    }

	    #endregion
	}
}
