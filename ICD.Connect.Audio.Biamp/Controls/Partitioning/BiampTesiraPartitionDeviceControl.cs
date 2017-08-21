using ICD.Common.EventArguments;
using ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks.RoomCombiner;
using ICD.Connect.Partitioning.Controls;

namespace ICD.Connect.Audio.Biamp.Controls.Partitioning
{
	public sealed class BiampTesiraPartitionDeviceControl : AbstractPartitionDeviceControl<BiampTesiraDevice>
	{
		private readonly RoomCombinerWall m_Wall;
		private readonly string m_Name;

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return m_Name; } }

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

			IsOpen = !wall.WallClosed;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Wall);
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
			IsOpen = !args.Data;
		}

		#endregion
	}
}
