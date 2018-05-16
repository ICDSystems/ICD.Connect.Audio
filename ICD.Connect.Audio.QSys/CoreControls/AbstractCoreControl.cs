using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.QSys.CoreControls
{
    /// <summary>
    /// Represents a control on the QSys Core, either a NamedControl or a NamedComponent
    /// </summary>
	public abstract class AbstractCoreControl : IConsoleNode, IStateDisposable, IQSysCoreControl
    {
	    private readonly QSysCoreDevice m_Core;

	    private readonly string m_Name;

	    private readonly int m_Id;

	    public QSysCoreDevice QSysCore
	    {
		    get { return m_Core; }
	    }

	    public string Name
	    {
		    get { return m_Name; }
	    }

	    public int Id
	    {
		    get { return m_Id; }
	    }

	    protected void SendData(string data)
        {
			m_Core.SendData(data);
        }

        protected AbstractCoreControl(QSysCoreDevice qSysCore, string name, int id)
        {
	        m_Core = qSysCore;
	        m_Id = id;
			m_Name = name;
		}

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
	    public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
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

		public void Dispose()
	    {
		    DisposeFinal(true);
	    }

	    public virtual void DisposeFinal(bool disposing)
	    {
		    
	    }

	    /// <summary>
	    /// Returns true if this instance has been disposed.
	    /// </summary>
	    public bool IsDisposed { get; private set; }
    }
}
