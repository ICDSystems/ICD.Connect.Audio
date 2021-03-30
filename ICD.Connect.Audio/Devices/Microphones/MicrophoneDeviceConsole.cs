using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.Devices.Microphones
{
	public static class MicrophoneDeviceConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IMicrophoneDevice instance)
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
		public static void BuildConsoleStatus(IMicrophoneDevice instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Is Muted", instance.IsMuted);
			addRow("Phantom Power", instance.PhantomPower);
			addRow("Gain Level", instance.GainLevel);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IMicrophoneDevice instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<bool>("SetMuted", "SetMuted <TRUE/FALSE>", b => instance.SetMuted(b));
			yield return new GenericConsoleCommand<bool>("SetPhantomPower", "SetPhantomPower <TRUE/FALSE>", b => instance.SetPhantomPower(b));
			yield return new GenericConsoleCommand<float>("SetGainLevel", "SetGainLevel <LEVEL>", f => instance.SetGainLevel(f));
		}
	}
}
