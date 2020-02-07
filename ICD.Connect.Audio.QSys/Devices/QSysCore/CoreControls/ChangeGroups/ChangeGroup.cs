using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.ChangeGroups
{
	public sealed class ChangeGroup : AbstractCoreControl, IChangeGroup
	{
		private const float DEFAULT_POLL_INTERVAL = 0.5f;

		private readonly IcdHashSet<INamedControl> m_NamedControls;
		private readonly Dictionary<INamedComponent, IcdHashSet<INamedComponentControl>> m_NamedComponents;
		private readonly SafeCriticalSection m_CriticalSection;

		#region Properties

		public string ChangeGroupId { get; private set; }

		public float? PollInterval { get; private set; }

		#endregion

		/// <summary>
		/// Constructor for Explicitly Defined Change Groups
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public ChangeGroup(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, friendlyName, id)
		{
			m_NamedControls = new IcdHashSet<INamedControl>();
			m_NamedComponents = new Dictionary<INamedComponent, IcdHashSet<INamedComponentControl>>();
			m_CriticalSection = new SafeCriticalSection();

			ChangeGroupId = XmlUtils.ReadChildElementContentAsString(xml, "ChangeGroupId");

			float? pollInterval = XmlUtils.TryReadChildElementContentAsFloat(xml, "PollInterval");
			if (pollInterval.HasValue)
				PollInterval = pollInterval.Value;
		}

		/// <summary>
		/// Constructor for Implicitly Defined Change Groups
		/// </summary>
		/// <param name="id"></param>
		/// <param name="context"></param>
		/// <param name="changeGroupId"></param>
		[UsedImplicitly]
		public ChangeGroup(int id, CoreElementsLoadContext context, string changeGroupId)
			: base(context.QSysCore, string.Format("Implicit Change Group {0}", changeGroupId), id)
		{
			m_NamedControls = new IcdHashSet<INamedControl>();
			m_NamedComponents = new Dictionary<INamedComponent, IcdHashSet<INamedComponentControl>>();
			m_CriticalSection = new SafeCriticalSection();

			ChangeGroupId = changeGroupId;
			PollInterval = DEFAULT_POLL_INTERVAL;
		}

		#region Methods

		public void AddNamedControl(INamedControl control)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			AddNamedControls(control.Yield());
		}

		public void AddNamedControls(IEnumerable<INamedControl> controls)
		{
			if (controls == null)
				throw new ArgumentNullException("controls");

			IcdHashSet<INamedControl> addedControls = new IcdHashSet<INamedControl>();
			bool firstItem;

			m_CriticalSection.Enter();
			
			try
			{
				firstItem = m_NamedControls.Count == 0;

				foreach (INamedControl control in controls)
				{
					if (m_NamedControls.Add(control))
						addedControls.Add(control);
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			if (addedControls.Count == 0 || !QSysCore.Initialized)
				return;

			SendData(new ChangeGroupAddControlRpc(this, addedControls));

			if (firstItem)
				SetAutoPoll();
		}

		public void AddNamedComponent(INamedComponent component)
		{
			if (component == null)
				throw new ArgumentNullException("component");

			AddNamedComponent(component, component.GetControls());
		}

		public void AddNamedComponent(INamedComponent component, IEnumerable<INamedComponentControl> controls)
		{
			if (component == null)
				throw new ArgumentNullException("component");

			if (controls == null)
				throw new ArgumentNullException("controls");

			IcdHashSet<INamedComponentControl> addedControls = new IcdHashSet<INamedComponentControl>();

			m_CriticalSection.Enter();

			try
			{
				// If component isn't in dict, add it
				IcdHashSet<INamedComponentControl> cache = m_NamedComponents.GetOrAddNew(component);

				// Add controls to component
				foreach (INamedComponentControl control in controls)
				{
					if (cache.Add(control))
						addedControls.Add(control);
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			// Send subscribe to Core
			if (addedControls.Count > 0 && QSysCore.Initialized)
				SendData(new ChangeGroupAddComponentControlRpc(this, component, addedControls));
		}

		public IEnumerable<INamedControl> GetControls()
		{
			return m_CriticalSection.Execute(() => m_NamedControls.ToArray());
		}

		public void Initialize()
		{
			// Send Named Controls
			SendData(new ChangeGroupAddControlRpc(this, GetControls()));

			// Send Named Components
			foreach (KeyValuePair<INamedComponent, IcdHashSet<INamedComponentControl>> kvp in
				m_CriticalSection.Execute(() => m_NamedComponents.ToArray()))
				SendData(new ChangeGroupAddComponentControlRpc(this, kvp.Key, kvp.Value));

			// Setup Auto-Polling
			SendData(new ChangeGroupAutoPollRpc(this));
			SendData(new ChangeGroupPollRpc(this));
		}

		public void DestroyChangeGroup()
		{
			if (QSysCore.Initialized)
				SendData(new ChangeGroupDestroyRpc(this));
		}

		#endregion

		#region Private Methods

		private void SetAutoPoll()
		{
			if (PollInterval != null && QSysCore.Initialized)
				SendData(new ChangeGroupAutoPollRpc(this));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintComponentControls", "Prints a table of the named controls for each named component", () => PrintComponentControls());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.IndexNodeMap("NamedControls", m_NamedControls.OrderBy(n => n.Name));
			yield return ConsoleNodeGroup.IndexNodeMap("NamedComponents", m_NamedComponents.Keys.OrderBy(n => n.Name));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		private string PrintComponentControls()
		{
			TableBuilder builder = new TableBuilder("Named Component", "Named Component Controls");

			m_CriticalSection.Enter();

			try
			{
				bool first = true;

				foreach (KeyValuePair<INamedComponent, IcdHashSet<INamedComponentControl>> kvp in m_NamedComponents.OrderBy(kvp => kvp.Key.Name))
				{
					if (!first)
						builder.AddSeparator();
					first = false;

					string name = kvp.Key.Name;

					foreach (INamedComponentControl value in kvp.Value.OrderBy(v => v.Name))
					{
						builder.AddRow(name, value.Name);
						name = null;
					}
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			return builder.ToString();
		}

		#endregion
	}
}
