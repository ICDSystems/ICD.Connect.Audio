using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls
{
	/// <summary>
	/// Represents a control on the QSys Core, either a NamedControl or a NamedComponent
	/// </summary>
	public abstract class AbstractCoreControl : IConsoleNode, IStateDisposable, IQSysCoreControl
	{
		private readonly QSysCoreDevice m_Core;
		private readonly string m_Name;
		private readonly int m_Id;

		#region Properties

		public QSysCoreDevice QSysCore { get { return m_Core; } }

		public int Id { get { return m_Id; } }

		public string Name { get { return m_Name; } }

		/// <summary>
		/// Returns true if this instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="qSysCore"></param>
		/// <param name="name"></param>
		/// <param name="id"></param>
		protected AbstractCoreControl(QSysCoreDevice qSysCore, string name, int id)
		{
			m_Core = qSysCore;
			m_Id = id;
			m_Name = name;
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AbstractCoreControl()
		{
			DisposeFinal(false);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			DisposeFinal(true);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		public virtual void DisposeFinal(bool disposing)
		{
		}

		/// <summary>
		/// Sends the given data string to the device.
		/// </summary>
		/// <param name="data"></param>
		protected void SendData(string data)
		{
			m_Core.SendData(data);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public virtual string ConsoleName { get { return Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return "QSys Core Control"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Id", Id);
			addRow("Name", Name);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}
