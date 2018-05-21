using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Audio.QSys.Rpc;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Audio.QSys.CoreControls.NamedComponents
{
	public sealed class NamedComponentControl: INamedComponentControl
	{
		private INamedComponent m_Component;

		public string ValueString { get; private set; }

		public float ValueRaw { get; private set; }

		public float ValuePosition { get; private set; }

		public event EventHandler<ControlValueUpdateEventArgs> OnValueUpdated;

		public string Name { get; private set; }

		public void ParseFeedback(JToken feedback)
		{
			ValueRaw = float.Parse((string)feedback.SelectToken("Value"));
			ValueString = (string)feedback.SelectToken("String");
			ValuePosition = float.Parse((string)feedback.SelectToken("Position"));

			OnValueUpdated.Raise(this, new ControlValueUpdateEventArgs(Name, ValueString, ValueRaw, ValuePosition));

		}

		public NamedComponentControl(INamedComponent component, string name)
		{
			m_Component = component;
			Name = name;
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return String.Format("ComponentControl:{0}", Name); } }

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
			//yield return new ConsoleCommand("Trigger", "Triggers the control", () => TriggerControl());
			//yield return new GenericConsoleCommand<string>("SetPosition", "SetPosition <Position>", p => SetPosition(float.Parse(p)));
			yield return new GenericConsoleCommand<string>("SetValue", "SetValue <Value>", p => SetValue(p));
		}

		public void SetValue(string value)
		{
			m_Component.QSysCore.SendData(new ComponentSetRpc(m_Component.ComponentName, Name, value).Serialize());
		}

		public void SetPosition(float parse)
		{
			// todo: implement ComponentSetValueRpc
			throw new NotImplementedException();
		}

		public void TriggerControl()
		{
			// todo: Change to set position 1
			SetValue("1");
		}

		private void PollValue()
		{
			m_Component.QSysCore.SendData(new ComponentGetRpc(m_Component.ComponentName, Name).Serialize());
		}

		#endregion

	}
}