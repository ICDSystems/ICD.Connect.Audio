using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Audio.Biamp.Tesira.Controls.State
{
	public abstract class AbstractBiampTesiraStateDeviceControl : AbstractDeviceControl<BiampTesiraDevice>, IBiampTesiraStateDeviceControl
	{
		/// <summary>
		/// Raised when the state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnStateChanged;

		private readonly string m_Name;
		private bool m_State;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the state of the control.
		/// </summary>
		public virtual bool State
		{
			get { return m_State; }
			protected set
			{
				if (value == m_State)
					return;

				m_State = value;

				OnStateChanged.Raise(this, new BoolEventArgs(m_State));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		/// <param name="name"></param>
		/// <param name="device"></param>
		protected AbstractBiampTesiraStateDeviceControl(int id, Guid uuid, string name, BiampTesiraDevice device)
			: base(device, id, uuid)
		{
			m_Name = name;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnStateChanged = null;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state"></param>
		public abstract void SetState(bool state);

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetState", "SetState <true/false>", v => SetState(v));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("State", State);
		}

		#endregion
	}
}
