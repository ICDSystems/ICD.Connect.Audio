using ICD.Connect.Audio.Biamp.Tesira.Controls;

namespace ICD.Connect.Audio.Biamp.Tesira.AttributeInterfaces
{
	public interface IAttributeInterface
	{
		/// <summary>
		/// Gets the parent device for this component.
		/// </summary>
		BiampTesiraDevice Device { get; }

		/// <summary>
		/// Gets the child attribute interface at the given path.
		/// </summary>
		/// <param name="channelType"></param>
		/// <param name="indices"></param>
		/// <returns></returns>
		IAttributeInterface GetAttributeInterface(eChannelType channelType, params int[] indices);

		/// <summary>
		/// Subscribe to feedback from the system.
		/// </summary>
		void Subscribe();
	}
}
