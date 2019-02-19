using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.QSys.CoreControls.NamedControls;
using ICD.Connect.Partitioning.Controls;

namespace ICD.Connect.Audio.QSys.Controls.Partitioning
{
	public sealed class QSysPartitionControl : AbstractPartitionDeviceControl<QSysCoreDevice>, IQSysKrangControl
	{
		private readonly BooleanNamedControl m_PartitionControl;
		private readonly string m_Name;

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.IsNullOrEmpty(m_Name) ? base.Name : m_Name; } }

		/// <summary>
		/// Constructor used to load control from xml
		/// </summary>
		/// <param name="id"></param>
		/// <param name="friendlyName"></param>
		/// <param name="context"></param>
		/// <param name="xml"></param>
		[UsedImplicitly]
		public QSysPartitionControl(int id, string friendlyName, CoreElementsLoadContext context, string xml)
			: base(context.QSysCore, id)
		{
			m_Name = friendlyName;

			string partitionControlName = XmlUtils.TryReadChildElementContentAsString(xml, "PartitionControlName");
			m_PartitionControl = context.LazyLoadNamedControl<BooleanNamedControl>(partitionControlName);

			Subscribe(m_PartitionControl);

			IsOpen = m_PartitionControl != null && m_PartitionControl.ValueBool;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_PartitionControl);
		}

		#region Methods

		/// <summary>
		/// Opens the partition.
		/// </summary>
		public override void Open()
		{
			m_PartitionControl.SetValue(true);
		}

		/// <summary>
		/// Closes the partition.
		/// </summary>
		public override void Close()
		{
			m_PartitionControl.SetValue(false);
		}

		#endregion

		#region Control Callbacks 

		private void Subscribe(BooleanNamedControl partitionControl)
		{
			if (partitionControl == null)
				return;

			partitionControl.OnValueUpdated += PartitionControlOnValueUpdated;
		}

		private void Unsubscribe(BooleanNamedControl partitionControl)
		{
			if (partitionControl == null)
				return;

			partitionControl.OnValueUpdated -= PartitionControlOnValueUpdated;
		}

		private void PartitionControlOnValueUpdated(object sender, ControlValueUpdateEventArgs controlValueUpdateEventArgs)
		{
			IsOpen = m_PartitionControl.ValueBool;
		}

		#endregion
	}
}
