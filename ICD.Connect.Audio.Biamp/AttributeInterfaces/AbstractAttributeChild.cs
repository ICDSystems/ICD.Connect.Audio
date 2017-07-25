using ICD.Common.Services.Logging;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces
{
	/// <summary>
	/// Base class for attribute children. E.g. an AudioInputBlock has child AudioInputChannels.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class AbstractAttributeChild<T> : AbstractAttributeInterface
		where T : AbstractAttributeInterface
	{
		private readonly T m_Parent;
		private readonly int m_Index;

		/// <summary>
		/// Gets the parent attribute interface.
		/// </summary>
		protected T Parent { get { return m_Parent; } }

		/// <summary>
		/// Gets the index of this child.
		/// </summary>
		public int Index { get { return m_Index; } }

		/// <summary>
		/// Gets the name of the index, used with logging.
		/// </summary>
		protected abstract string IndexName { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index"></param>
		protected AbstractAttributeChild(T parent, int index)
			: base(parent.Device, parent.InstanceTag)
		{
			m_Parent = parent;
			m_Index = index;
		}

		/// <summary>
		/// Logs the message with the device configured logger.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public sealed override void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} {1} - {2}", IndexName, Index, message);
			Parent.Log(severity, message, args);
		}
	}
}
