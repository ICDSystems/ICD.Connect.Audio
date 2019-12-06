using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;

namespace ICD.Connect.Audio.Console.Volume
{
	public static class VolumeDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes([NotNull] IVolumeDeviceControl instance)
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
		public static void BuildConsoleStatus([NotNull] IVolumeDeviceControl instance, [NotNull] AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (addRow == null)
				throw new ArgumentNullException("addRow");

			string[] supportedArray =
				EnumUtils.GetFlagsExceptNone(instance.SupportedVolumeFeatures)
				         .Select(f => f.ToString())
				         .ToArray();

			string supported = string.Join(", ", supportedArray);

			addRow("Supported Volume Features", supported);
			addRow("Is Muted", instance.IsMuted);
			addRow("Volume Level", instance.VolumeLevel);
			addRow("Volume Level Min", instance.VolumeLevelMin);
			addRow("Volume Level Max", instance.VolumeLevelMax);
			addRow("Volume String", instance.VolumeString);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands([NotNull] IVolumeDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<bool>("SetIsMuted", "SetIsMuted <true/false>", m => instance.SetIsMuted(m));
			yield return new ConsoleCommand("ToggleIsMuted", "Toggles the current mute state.", () => instance.ToggleIsMuted());
			yield return new GenericConsoleCommand<float>("SetVolumeLevel", "SetVolumeLevel <LEVEL>", r => instance.SetVolumeLevel(r));
			yield return new ConsoleCommand("VolumeIncrement", "Raises the volume one time", () => instance.VolumeIncrement());
			yield return new ConsoleCommand("VolumeDecrement", "Lowers the volume one time", () => instance.VolumeDecrement());

			yield return new GenericConsoleCommand<bool, long>(
				"VolumeRamp <UP (true/false)> <TIMEOUT (ms)>",
				"Starts ramping the volume, and continues until stop is called.",
				(a, c) => instance.VolumeRamp(a, c));

			yield return new ConsoleCommand("VolumeRampStop", "Stops any current ramp up/down in progress.", () => instance.VolumeRampStop());
		}
	}
}
