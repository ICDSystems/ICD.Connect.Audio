using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls;

namespace ICD.Connect.Audio.Console
{
	public static class VolumeRampDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IVolumeRampDeviceControl instance)
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
		public static void BuildConsoleStatus(IVolumeRampDeviceControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IVolumeRampDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("VolumeLevelIncrement", "Raises the volume one time", () => instance.VolumeIncrement());
			yield return new ConsoleCommand("VolumeLevelDecrement", "Lowers the volume one time", () => instance.VolumeDecrement());
			yield return new ConsoleCommand("VolumeLevelRampUp", "Starts raising the volume, and continues until RampStop is called.", () => instance.VolumeRampUp());
			yield return new ConsoleCommand("VolumeLevelRampDown", "Starts lowering the volume, and continues until RampStop is called.", () => instance.VolumeRampDown());
			yield return new ConsoleCommand("VolumeLevelRampStop", "Stops any current ramp up/down in progress.", () => instance.VolumeRampStop());
		}
	}
}
