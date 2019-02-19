using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Controls;
using ICD.Connect.Audio.QSys.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Rpc;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
	public abstract class AbstractNamedComponent : AbstractCoreControl, INamedComponent
	{

		private readonly Dictionary<string, INamedComponentControl> m_NamedComponentControls;

		private readonly SafeCriticalSection m_NamedComponentControlsCriticalSection;

		/// <summary>
		/// Component Name in QSys
		/// </summary>
		public string ComponentName { get; protected set; }

		/// <summary>
		/// Constructor to be called by concretes
		/// </summary>
		/// <param name="qSysCore"></param>
		/// <param name="name"></param>
		/// <param name="id"></param>
		protected AbstractNamedComponent(QSysCoreDevice qSysCore, string name, int id) : base(qSysCore, name, id)
		{
			m_NamedComponentControls = new Dictionary<string, INamedComponentControl>();
			m_NamedComponentControlsCriticalSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Subscribes this control to the initial change groups.
		/// </summary>
		/// <param name="loadContext">Load context, to lookup change groups and global default change groups</param>
		/// <param name="controlChangeGroups">Additional change groups needed for this control</param>
		protected void SetupInitialChangeGroups(CoreElementsLoadContext loadContext,
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
					changeGroup.AddNamedComponent(this, GetControlsForSubscribe());
				else
					QSysCore.Log(eSeverity.Warning, "NamedComponent {0} couldn't add to change group id {1} - not found", Id,
					             changeGroupId);
			}
		}

		/// <summary>
		/// Parse feedback from QSys
		/// </summary>
		/// <param name="feedback"></param>
		public void ParseFeedback(JToken feedback)
		{
			string name = (string)feedback.SelectToken("Name");

			if (string.IsNullOrEmpty(name))
			{
				return;
			}

			INamedComponentControl control;

			m_NamedComponentControlsCriticalSection.Enter();
			try
			{
				if (!m_NamedComponentControls.TryGetValue(name, out control))
					return;
			}
			finally
			{
				m_NamedComponentControlsCriticalSection.Leave();
			}

			control.ParseFeedback(feedback);
		}

		#region ComponentControls

		/// <summary>
		/// Send a get command to get the current values of all controls registered in the component
		/// </summary>
		internal void PollControls()
		{
			IEnumerable<string> controlNames = Enumerable.Empty<string>();
			m_NamedComponentControlsCriticalSection.Execute(() => controlNames = m_NamedComponentControls.Keys);
			SendData(new ComponentGetRpc(ComponentName, controlNames).Serialize());
		}

		internal void AddControl(NamedComponentControl control)
		{
			m_NamedComponentControlsCriticalSection.Execute(() => m_NamedComponentControls[control.Name] = control);
		}

		public IEnumerable<INamedComponentControl> GetControls()
		{
			IEnumerable<INamedComponentControl> controls = Enumerable.Empty<INamedComponentControl>();
			m_NamedComponentControlsCriticalSection.Execute(
			                                                () => controls =
				                                                      m_NamedComponentControls.Values.ToArray(
				                                                                                              m_NamedComponentControls
					                                                                                              .Count));
			return controls;
		}

		/// <summary>
		/// What controls are subscribed by default.  
		/// </summary>
		/// <returns></returns>
		internal virtual IEnumerable<INamedComponentControl> GetControlsForSubscribe()
		{
			return GetControls();
		}

		public INamedComponentControl GetControl(string controlName)
		{
			INamedComponentControl control = TryGetControl(controlName);
			if (control == null)
				throw new KeyNotFoundException(String.Format("NamedComponent {0} Control name {1} not found", this, controlName));
			return control;
		}

		public INamedComponentControl TryGetControl(string controlName)
		{
			INamedComponentControl control = null;
			m_NamedComponentControlsCriticalSection.Execute(() => m_NamedComponentControls
				                                                .TryGetValue(controlName, out control));
			return control;
		}

		#endregion

		#region Console

		/// <summary>
		/// Builds the status element for console
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
			addRow("ComponentName", ComponentName);
		}

		/// <summary>
		/// Gets the console commands for the node
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;
			
			yield return new ConsoleCommand("PollControls", "Poll all controls registered by the component", () => PollControls());

		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;
			yield return ConsoleNodeGroup.IndexNodeMap("Controls", GetControls());
		}

		/// <summary>
		/// Gets the base's console commands
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets base's console nodes
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion

	}
}