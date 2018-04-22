using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.QSys.CoreControls.NamedComponents;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.QSys.CoreControls
{
	/// <summary>
	/// Context to load the core controls in
	/// Note: Not Thread Safe
	/// </summary>
    internal class CoreControlsLoadContext
    {
		#region Constants

	    private const int NEXT_ID_START = 1000;

		#endregion


		#region Fields

	    /// <summary>
	    /// Contorls ID to Control Type
	    /// </summary>
	    private Dictionary<int, Type> m_ControlsTypes;

		/// <summary>
		/// Controls ID to XML String
		/// </summary>
		private Dictionary<int, string> m_ControlsXml;

		/// <summary>
		/// Named Controls by String
		/// </summary>
	    private Dictionary<string, int> m_NamedControls;

		/// <summary>
		/// Named Components by String
		/// </summary>
	    private Dictionary<string, int> m_NamedComponents;

		/// <summary>
		/// Next Avaliable ID to try for a control
		/// </summary>
	    private int m_NextAvaliableId;

		/// <summary>
		/// Default change groups to subscribe to
		/// </summary>
	    private List<int> m_DefaultChangeGroups;

		#endregion

		#region Properties

		/// <summary>
		/// Core for controls in load context
		/// </summary>
	    internal QSysCoreDevice QSysCore { get; }

	    internal IEnumerable<KeyValuePair<int, Type>> ControlsTypes
	    {
		    get { return m_ControlsTypes.ToList(m_ControlsTypes.Count);}
	    }

	    internal IEnumerable<KeyValuePair<int, string>> ControlsXml
	    {
		    get { return m_ControlsXml.ToList(m_ControlsXml.Count); }
	    }

	    #endregion

		#region Constructor

		public CoreControlsLoadContext(QSysCoreDevice qSysCore)
	    {
		    QSysCore = qSysCore;
		    m_NextAvaliableId = NEXT_ID_START;
	    }

		#endregion


		#region Methods

	    /// <summary>
	    /// Adds a generic control
	    /// </summary>
	    /// <param name="id"></param>
	    /// <param name="controlType"></param>
	    /// <param name="xml"></param>
	    internal void AddControl(int id, Type controlType, string xml)
	    {
		    m_ControlsTypes[id] = controlType;
		    m_ControlsXml[id] = xml;
	    }

		/// <summary>
		/// Links a Named control
		/// </summary>
		/// <param name="name"></param>
		/// <param name="id"></param>
	    internal void LinkNamedControl(string name, int id)
	    {
		    m_NamedControls[name] = id;
	    }

		/// <summary>
		/// Links a named conmponent
		/// </summary>
		/// <param name="name"></param>
		/// <param name="id"></param>
	    internal void LinkNamedComponent(string name, int id)
	    {
		    m_NamedComponents[name] = id;
	    }

		/// <summary>
		/// Gets the next avaliable ID from the list
		/// </summary>
		/// <returns></returns>
	    internal int GetNextId()
		{
			int checkValue = m_NextAvaliableId;
			while (true)
			{
				if (!m_ControlsTypes.ContainsKey(checkValue))
				{
					m_NextAvaliableId = checkValue + 1;
					return checkValue;
				}
				checkValue++;
			}
		}

		/// <summary>
		/// Get the type string for a control
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
	    internal Type GetTypeForId(int id)
	    {
		    Type typeString;
		    if (!m_ControlsTypes.TryGetValue(id, out typeString))
			    return null;
		    return typeString;
	    }


	    internal string GetXmlForId(int id)
	    {
		    string xml;
		    if (!m_ControlsXml.TryGetValue(id, out xml))
			    return null;
		    return xml;
	    }

	    #endregion

		/// <summary>
		/// Adds the ID as a default change group
		/// </summary>
		/// <param name="id"></param>
	    public void AddDefaultChangeGroup(int id)
	    {
		    m_DefaultChangeGroups.Add(id);
	    }
    }
}
