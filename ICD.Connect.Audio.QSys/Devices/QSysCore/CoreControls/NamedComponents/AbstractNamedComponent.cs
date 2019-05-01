using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public abstract class AbstractNamedComponent : AbstractCoreControl, INamedComponent
	{
		/// <summary>
		/// Raised when a component control value changes.
		/// </summary>
		public event EventHandler<ControlValueUpdateEventArgs> OnControlValueUpdated;

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
		protected AbstractNamedComponent(QSysCoreDevice qSysCore, string name, int id)
			: base(qSysCore, name, id)
		{
			m_NamedComponentControls = new Dictionary<string, INamedComponentControl>();
			m_NamedComponentControlsCriticalSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnControlValueUpdated = null;

			base.DisposeFinal(disposing);

			ClearControls();
		}

		/// <summary>
		/// Parse feedback from QSys
		/// </summary>
		/// <param name="feedback"></param>
		public void ParseFeedback(JToken feedback)
		{
			string name = (string)feedback.SelectToken("Name");
			if (name == null)
				return;

			INamedComponentControl control =
				m_NamedComponentControlsCriticalSection.Execute(() => m_NamedComponentControls.GetDefault(name));

			if (control != null)
				control.ParseFeedback(feedback);
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
					changeGroup.AddNamedComponent(this, GetControls());
				else
					QSysCore.Log(eSeverity.Warning, "NamedComponent {0} couldn't add to change group id {1} - not found", Id,
					             changeGroupId);
			}
		}

		#region Component Controls

		/// <summary>
		/// Send a get command to get the current values of all controls registered in the component
		/// </summary>
		private void PollControls()
		{
			IEnumerable<string> controlNames = Enumerable.Empty<string>();
			m_NamedComponentControlsCriticalSection.Execute(() => controlNames = m_NamedComponentControls.Keys.ToArray());
			SendData(new ComponentGetRpc(ComponentName, controlNames));
		}

		/// <summary>
		/// Adds controls for the given control names.
		/// </summary>
		/// <param name="controlNames"></param>
		protected void AddControls(IEnumerable<string> controlNames)
		{
			if (controlNames == null)
				throw new ArgumentNullException("controlNames");

			m_NamedComponentControlsCriticalSection.Enter();

			try
			{
				foreach (string controlName in controlNames)
				{
					if (m_NamedComponentControls.ContainsKey(controlName))
						throw new InvalidOperationException(string.Format("Already contains a named control with name {0}", controlName));

					INamedComponentControl control = new NamedComponentControl(this, controlName);
					m_NamedComponentControls.Add(controlName, control);

					Subscribe(control);
				}
			}
			finally
			{
				m_NamedComponentControlsCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Unsubscribes and clears all of the added named component controls.
		/// </summary>
		private void ClearControls()
		{
			m_NamedComponentControlsCriticalSection.Enter();

			try
			{
				foreach (var value in m_NamedComponentControls.Values)
					Unsubscribe(value);

				m_NamedComponentControls.Clear();
			}
			finally
			{
				m_NamedComponentControlsCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets the child named component controls.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<INamedComponentControl> GetControls()
		{
			return m_NamedComponentControlsCriticalSection.Execute(
				() => m_NamedComponentControls.Values.ToArray(m_NamedComponentControls.Count));
		}

		/// <summary>
		/// Gets the named component control with the given name.
		/// </summary>
		/// <param name="controlName"></param>
		/// <returns></returns>
		public INamedComponentControl GetControl(string controlName)
		{
			m_NamedComponentControlsCriticalSection.Enter();

			try
			{
				INamedComponentControl control;
				if (!m_NamedComponentControls.TryGetValue(controlName, out control))
					throw new KeyNotFoundException(string.Format("NamedComponent {0} Control name {1} not found", this, controlName));

				return control;
			}
			finally
			{
				m_NamedComponentControlsCriticalSection.Leave();
			}
		}

		public void SetValue(string controlName, string value)
		{
			GetControl(controlName).SetValue(value);
		}

		public void Trigger(string controlName)
		{
			GetControl(controlName).Trigger();
		}

		#endregion

		#region Controls

		/// <summary>
		/// Subscribe to the control events.
		/// </summary>
		/// <param name="control"></param>
		private void Subscribe(INamedComponentControl control)
		{
			control.OnValueUpdated += ControlOnValueUpdated;
		}

		/// <summary>
		/// Unsubscribe from the control events.
		/// </summary>
		/// <param name="control"></param>
		private void Unsubscribe(INamedComponentControl control)
		{
			control.OnValueUpdated -= ControlOnValueUpdated;
		}

		/// <summary>
		/// Called when the control value changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ControlOnValueUpdated(object sender, ControlValueUpdateEventArgs eventArgs)
		{
			OnControlValueUpdated.Raise(sender, eventArgs);
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

			yield return
				new ConsoleCommand("PollControls", "Poll all controls registered by the component", () => PollControls());
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
