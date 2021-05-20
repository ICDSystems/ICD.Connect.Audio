using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.Devices.QSysCore.Rpc;
using ICD.Connect.Audio.QSys.EventArgs;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.Devices.QSysCore.CoreControls.NamedComponents
{
	public sealed class NamedComponentControl: INamedComponentControl
	{
		public event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		private readonly INamedComponent m_Component;

		#region Properties

		public string ValueString { get; private set; }

		public float ValueRaw { get; private set; }

		public float ValuePosition { get; private set; }

		public string Name { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="component"></param>
		/// <param name="name"></param>
		public NamedComponentControl(INamedComponent component, string name)
		{
			m_Component = component;
			Name = name;
		}

		public void ParseFeedback(JToken feedback)
		{
			ValueRaw = (float)feedback.SelectToken("Value");
			ValueString = (string)feedback.SelectToken("String");
			ValuePosition = (float)feedback.SelectToken("Position");

			OnValueUpdated.Raise(this, new ControlValueUpdateEventArgs(Name, ValueString, ValueRaw, ValuePosition));
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return string.Format("ComponentControl:{0}", Name); } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Named Component Control"; } }

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
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("ValueRaw", ValueRaw);
			addRow("ValueString", ValueString);
			addRow("ValuePosition", ValuePosition);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Poll", "Pull The Current Value", () => PollValue());
			//yield return new ConsoleCommand("Trigger", "Triggers the control", () => Trigger());
			//yield return new GenericConsoleCommand<string>("SetPosition", "SetPosition <Position>", p => SetPosition(float.Parse(p)));
			yield return new GenericConsoleCommand<string>("SetValue", "SetValue <Value>", p => SetValue(p));
		}

		public void SetValue(string value)
		{
			m_Component.QSysCore.SendData(new ComponentSetRpc(m_Component.ComponentName, Name, value));
		}

		public void SetValue(bool value)
		{
			m_Component.QSysCore.SendData(new ComponentSetRpc(m_Component.ComponentName, Name, value));
		}

		public void SetPosition(float parse)
		{
			// todo: implement ComponentSetValueRpc
			throw new NotImplementedException();
		}

		public void Trigger()
		{
			// todo: Change to set position 1
			SetValue("1");
		}

		private void PollValue()
		{
			m_Component.QSysCore.SendData(new ComponentGetRpc(m_Component.ComponentName, Name));
		}

		#endregion
	}
}