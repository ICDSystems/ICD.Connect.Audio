using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls;

namespace ICD.Connect.Audio.Console
{
	public static class VolumeLevelDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IVolumeLevelDeviceControl instance)
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
		public static void BuildConsoleStatus(IVolumeLevelDeviceControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("VolumeRaw", instance.VolumeRaw);
			addRow("VolumePosition", instance.VolumePosition);
			addRow("VolumeString", instance.VolumeString);
			addRow("VolumeRawMax", instance.VolumeRawMax);
			addRow("VolumeRawMin", instance.VolumeRawMin);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IVolumeLevelDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<float>("SetVolumeRaw", "SetVolumeRaw <Volume>", v => instance.SetVolumeRaw(v));
			yield return new GenericConsoleCommand<float>("SetVolumePosition", "SetVolumePosition <Position>", v => instance.SetVolumePosition(v));
			yield return new GenericConsoleCommand<float>("VolumeLevelIncrement", "VolumeLevelIncrement <Delta>", v => instance.VolumeLevelIncrement(v));
			yield return new GenericConsoleCommand<float>("VolumeLevelDecrement", "VolumeLevelDecrement <Delta>", v => instance.VolumeLevelDecrement(v));
		}
	}
}
