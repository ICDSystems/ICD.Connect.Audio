using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Controls;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public sealed class SnapshotNamedComponent : AbstractNamedComponent
	{
		private const int DEFAULT_SNAPSHOT_COUNT = 8;
		private const int MIN_SNAPSHOT = 1;
		private const int MAX_SNAPSHOT = 24;

		private int m_SnapshotCount = DEFAULT_SNAPSHOT_COUNT;

		/// <summary>
		/// Gets/sets the number of snapshots in this component.
		/// </summary>
		public int SnapshotCount
		{
			get { return m_SnapshotCount; }
			set { m_SnapshotCount = MathUtils.Clamp(value, MIN_SNAPSHOT, MAX_SNAPSHOT); }
		}

		/// <summary>
		/// Constructor for Explicitly defined component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public SnapshotNamedComponent(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, friendlyName, id)
		{
			string componentName = XmlUtils.TryReadChildElementContentAsString(xml, "ComponentName");

			// If we don't have a component name, bail out
			if (String.IsNullOrEmpty(componentName))
				throw new InvalidOperationException(
					string.Format("Tried to create SnapshotNamedComponent {0}:{1} without component name", id, friendlyName));

			SnapshotCount = XmlUtils.TryReadChildElementContentAsInt(xml, "SnapshotCount") ?? DEFAULT_SNAPSHOT_COUNT;

			ComponentName = componentName;
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		/// <summary>
		/// Constructor for Implicitly built component
		/// </summary>
		/// <param name="id"></param>
		/// <param name="context"></param>
		/// <param name="componentName"></param>
		[UsedImplicitly]
		public SnapshotNamedComponent(int id, CoreElementsLoadContext context, string componentName)
			: base(context.QSysCore, string.Format("Implicit:{0}", componentName), id)
		{
			ComponentName = componentName;
			SetupInitialChangeGroups(context, Enumerable.Empty<int>());
		}

		#region Methods

		/// <summary>
		/// Recalls the snapshot at the given bank.
		/// </summary>
		/// <param name="snapshot"></param>
		public void LoadSnapshot(int snapshot)
		{
			if (snapshot < MIN_SNAPSHOT || snapshot > MAX_SNAPSHOT)
				throw new ArgumentOutOfRangeException("snapshot");

			LoadSnapshot(snapshot, 0);
		}

		/// <summary>
		/// Recalls the snapshot at the given bank.
		/// </summary>
		/// <param name="snapshot"></param>
		/// <param name="ramp"></param>
		public void LoadSnapshot(int snapshot, int ramp)
		{
			if (snapshot < MIN_SNAPSHOT || snapshot > MAX_SNAPSHOT)
				throw new ArgumentOutOfRangeException("snapshot");

			SnapshotLoadRpc rpc = new SnapshotLoadRpc
			{
				Name = ComponentName,
				Bank = snapshot,
				Ramp = ramp == 0 ? (int?)null : ramp
			};

			SendData(rpc);
		}

		/// <summary>
		/// Saves the snapshot at the given bank.
		/// </summary>
		/// <param name="snapshot"></param>
		public void SaveSnapshot(int snapshot)
		{
			if (snapshot < MIN_SNAPSHOT || snapshot > MAX_SNAPSHOT)
				throw new ArgumentOutOfRangeException("snapshot");

			SnapshotSaveRpc rpc = new SnapshotSaveRpc
			{
				Name = ComponentName,
				Bank = snapshot
			};

			SendData(rpc);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public override string ConsoleName { get { return string.Format("SnapshotComponent:{0}", Name); } }

		/// <summary>
		/// Builds the status element for console
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Snapshot Count", SnapshotCount);
		}

		/// <summary>
		/// Gets the console commands for the node
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("LoadSnapshot", "LoadSnapshot <NUMBER>", i => LoadSnapshot(i));
			yield return new GenericConsoleCommand<int>("SaveSnapshot", "SaveSnapshot <NUMBER>", i => SaveSnapshot(i));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
