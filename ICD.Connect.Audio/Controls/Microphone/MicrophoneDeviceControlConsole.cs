using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Audio.Controls.Microphone
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

			addRow("Is Muted", instance.IsMuted);
			addRow("Phantom Power", instance.PhantomPower);
			addRow("Analog Gain Level", instance.AnalogGainLevel);
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

			yield return new GenericConsoleCommand<bool>("SetIsMuted", "SetIsMuted <TRUE/FALSE>", b => instance.SetIsMuted(b));
			yield return new GenericConsoleCommand<bool>("SetPhantomPower", "SetPhantomPower <TRUE/FALSE>", b => instance.SetPhantomPower(b));
			yield return new GenericConsoleCommand<float>("SetAnalogGainLevel", "SetAnalogGainLevel <LEVEL>", f => instance.SetAnalogGainLevel(f));
		}
	}
}
