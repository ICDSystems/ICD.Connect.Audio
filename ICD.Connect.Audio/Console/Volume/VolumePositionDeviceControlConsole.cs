using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Console.Volume
{
	public static class VolumePositionDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IVolumePositionDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IVolumePositionDeviceControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("VolumePosition", instance.VolumePosition);
			addRow("VolumeString", instance.VolumeString);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IVolumePositionDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<float>("SetVolumePosition", "SetVolumePosition <Position>", v => instance.SetVolumePosition(v));
		}
	}
}
