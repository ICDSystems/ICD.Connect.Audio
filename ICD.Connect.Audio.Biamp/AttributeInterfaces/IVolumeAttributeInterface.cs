using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces
{
	public interface IVolumeAttributeInterface : IAttributeInterface
	{
		[PublicAPI]
		event EventHandler<FloatEventArgs> OnLevelChanged;

		[PublicAPI]
		event EventHandler<BoolEventArgs> OnMuteChanged;

		#region Properties

		[PublicAPI]
		float Level { get; }

		[PublicAPI]
		bool Mute { get; }

		[PublicAPI]
		float AttributeMinLevel { get; }

		[PublicAPI]
		float AttributeMaxLevel { get; }

		#endregion

		#region Methods

		[PublicAPI]
		void SetLevel(float level);

		[PublicAPI]
		void IncrementLevel();

		[PublicAPI]
		void DecrementLevel();

		[PublicAPI]
		void SetMute(bool mute);

		#endregion
	}
}
