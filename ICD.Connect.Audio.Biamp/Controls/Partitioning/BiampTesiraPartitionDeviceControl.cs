using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Partitioning.Controls;

namespace ICD.Connect.Audio.Biamp.Controls.Partitioning
{
	public sealed class BiampTesiraPartitionDeviceControl : AbstractPartitionDeviceControl<BiampTesiraDevice>, IBiampTesiraDeviceControl
	{
		/// <summary>
		/// Raised when the partition is detected as open or closed.
		/// </summary>
		public override event EventHandler<BoolEventArgs> OnOpenStatusChanged;

		private readonly RoomCombinerWall m_Wall;
		private readonly string m_Name;

		private bool m_IsOpen;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Returns the current open state of the partition.
		/// </summary>
		public override bool IsOpen { get { return m_IsOpen; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="wall"></param>
		public BiampTesiraPartitionDeviceControl(int id, string name, RoomCombinerWall wall)
			: base(wall.Device, id)
		{
			m_Name = name;
			m_Wall = wall;

			Subscribe(m_Wall);

			UpdateIsOpen();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnOpenStatusChanged = null;

			Unsubscribe(m_Wall);

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Opens the partition.
		/// </summary>
		public override void Open()
		{
			m_Wall.SetWallClosed(false);
		}

		/// <summary>
		/// Closes the partition.
		/// </summary>
		public override void Close()
		{
			m_Wall.SetWallClosed(true);
		}

		#endregion

		#region Wall Callbacks

		/// <summary>
		/// Subscribes to the wall events.
		/// </summary>
		/// <param name="wall"></param>
		private void Subscribe(RoomCombinerWall wall)
		{
			wall.OnWallClosedChanged += WallOnWallClosedChanged;
		}

		/// <summary>
		/// Unsubscribes from the wall events.
		/// </summary>
		/// <param name="wall"></param>
		private void Unsubscribe(RoomCombinerWall wall)
		{
			wall.OnWallClosedChanged -= WallOnWallClosedChanged;
		}

		/// <summary>
		/// Called when the wall closed state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void WallOnWallClosedChanged(object sender, BoolEventArgs args)
		{
			UpdateIsOpen();
		}

		private void UpdateIsOpen()
		{
			bool open = !m_Wall.WallClosed;
			if (open == m_IsOpen)
				return;

			m_IsOpen = open;

			Log(eSeverity.Informational, "IsOpen changed to {0}", m_IsOpen);

			OnOpenStatusChanged.Raise(this, new BoolEventArgs(m_IsOpen));
		}

		#endregion
	}
}
