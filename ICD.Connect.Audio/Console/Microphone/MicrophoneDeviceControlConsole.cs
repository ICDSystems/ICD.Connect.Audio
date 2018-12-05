using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Microphone;

namespace ICD.Connect.Audio.Console.Microphone
{
	public static class MicrophoneDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IMicrophoneDeviceControl instance)
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
		public static void BuildConsoleStatus(IMicrophoneDeviceControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Gain Level", instance.GainLevel);
			addRow("Muted", instance.IsMuted);
			addRow("Phantom Power", instance.PhantomPower);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IMicrophoneDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<float>("SetGainLevel", "SetGainLevel <LEVEL>", l => instance.SetGainLevel(l));
			yield return new GenericConsoleCommand<bool>("SetMuted", "SetMuted <true/false>", b => instance.SetMuted(b));
			yield return
				new GenericConsoleCommand<bool>("SetPhantomPower", "SetPhantomPower <true/false>", b => instance.SetPhantomPower(b))
				;
		}
	}
}
