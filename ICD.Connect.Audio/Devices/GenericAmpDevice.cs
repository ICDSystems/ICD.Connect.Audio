using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Partitioning.VolumePoints;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Audio.Devices
{
	/// <summary>
	/// The GenericAmpDevice is essentially a mock audio switcher that provides a single
	/// volume control, representing the nearest, currently routed volume control.
	/// </summary>
	public sealed class GenericAmpDevice : AbstractDevice<GenericAmpDeviceSettings>
	{
		private readonly Dictionary<int, int> m_InputVolumePointIds;
		private readonly SafeCriticalSection m_InputsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public GenericAmpDevice()
		{
			m_InputVolumePointIds = new Dictionary<int, int>();
			m_InputsSection = new SafeCriticalSection();

			Controls.Add(new GenericAmpRouteSwitcherControl(this, 0));

			// Needs to be added after the route control
			Controls.Add(new GenericAmpVolumeControl(this, 1));
		}

		#region Methods

		/// <summary>
		/// Gets the volume point for the given input address.
		/// Returns null if no volume point is configured or the volume point could not be found.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		public IVolumePoint GetVolumePointForInput(int input)
		{
			int id;
			if (!TryGetVolumePointIdForInput(input, out id))
				return null;

			IOriginator originator;
			ServiceProvider.GetService<ICore>().Originators.TryGetChild(id, out originator);

			return originator as IVolumePoint;
		}

		/// <summary>
		/// Tries to get the volume point id for the given input address.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool TryGetVolumePointIdForInput(int input, out int id)
		{
			m_InputsSection.Enter();

			try
			{
				return m_InputVolumePointIds.TryGetValue(input, out id);
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the volume point id configured for each input.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<KeyValuePair<int, int>> GetInputVolumePointIds()
		{
			return m_InputsSection.Execute(() => m_InputVolumePointIds.ToArray(m_InputVolumePointIds.Count));
		}

		/// <summary>
		/// Sets the volume point id for each input.
		/// </summary>
		/// <param name="inputVolumePointIds"></param>
		[PublicAPI]
		public void SetInputVolumePointIds(IEnumerable<KeyValuePair<int, int>> inputVolumePointIds)
		{
			if (inputVolumePointIds == null)
				throw new ArgumentNullException("inputVolumePointIds");

			m_InputsSection.Enter();

			try
			{
				m_InputVolumePointIds.Clear();

				foreach (KeyValuePair<int, int> item in inputVolumePointIds)
				{
					if (m_InputVolumePointIds.ContainsKey(item.Key))
					{
						Logger.AddEntry(eSeverity.Error, "{0} unable to add volume point id for duplicate input {1}", this,
										item.Key);
						continue;
					}

					m_InputVolumePointIds.Add(item.Key, item.Value);
				}
			}
			finally
			{
				m_InputsSection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_InputVolumePointIds.Clear();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(GenericAmpDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SetInputVolumePointIds(GetInputVolumePointIds());
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(GenericAmpDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SetInputVolumePointIds(settings.GetInputVolumePointIds());
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PrintVolumePoints", "Prints a table of the configured volume points for each input",
			                                () => PrintVolumePoints());
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
		/// Builds a table of the configured volume points for each input.
		/// </summary>
		/// <returns></returns>
		private string PrintVolumePoints()
		{
			TableBuilder builder = new TableBuilder("Input", "Volume Point");

			foreach (int input in GetInputVolumePointIds().Select(kvp => kvp.Key))
				builder.AddRow(input, GetVolumePointForInput(input));

			return builder.ToString();
		}

		#endregion
	}
}
